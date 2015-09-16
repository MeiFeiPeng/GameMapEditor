﻿using GameMapEditor.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace GameMapEditor
{
    public partial class TilesetPanel : DockContent
    {
        private Point tilesetOrigin;
        private Rectangle tilesetSelection;
        private List<BitmapImageBundle> textures;
        
        private BitmapImageBundle currentTexture;
        private bool isGridActived;
        private Pen gridColor;
        private SolidBrush fillBrush;

        public event TilesetChangedEventHandler TilesetChanged;
        public delegate void TilesetChangedEventHandler(object sender, BitmapImageBundle texture);
        public event TilesetSelectionEventHandler TilesetSelectionChanged;
        public delegate void TilesetSelectionEventHandler(object sender, Rectangle selection);

        public static TilesetPanel Instance = new TilesetPanel();

        private TilesetPanel()
        {
            this.HideOnClose = true;
        }

        #region FrameEvents
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.InitializeComponent();

            this.tilesetOrigin = new Point();
            this.tilesetSelection = new Rectangle();
            this.textures = new List<BitmapImageBundle>();
            this.CursorColor = new Pen(Color.FromArgb(255, 0, 100, 0), 2);
            this.CursorColor.DashStyle = DashStyle.Dash;
            this.CursorColor.Alignment = PenAlignment.Inset;
            this.fillBrush = new SolidBrush(Color.FromArgb(50, 0, 50, 0));
            this.gridColor = new Pen(Color.FromArgb(130, 170, 170, 170), 1);
            this.IsSelectingTiles = false;
            this.IsGridActived = true;

            this.LoadTilesetFileList();
        }

        private void picTileset_Paint(object sender, PaintEventArgs e)
        {
            if (this.currentTexture != null)
            {
                e.Graphics.Clear(Color.WhiteSmoke);
            
                e.Graphics.DrawImage(this.currentTexture.BitmapSource, e.ClipRectangle,
                    this.tilesetOrigin.X,
                    this.tilesetOrigin.Y,
                    e.ClipRectangle.Width,
                    e.ClipRectangle.Height,
                    GraphicsUnit.Pixel);

                this.DrawGrid(this.isGridActived, e);

                Rectangle selection = new Rectangle(
                    this.TilesetSelection.Location.X - this.tilesetOrigin.X,
                    this.tilesetSelection.Location.Y - this.tilesetOrigin.Y,
                    this.tilesetSelection.Size.Width,
                    this.tilesetSelection.Size.Height);

                e.Graphics.FillRectangle(fillBrush, selection);
                e.Graphics.DrawRectangle(CursorColor, selection);
                
            }
        }

        private void picTileset_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.currentTexture != null)
            {
                Point pt = new Point(e.Location.X + this.TilesetOrigin.X, e.Location.Y + this.TilesetOrigin.Y);
                Rectangle rect = new Rectangle(Point.Empty, this.TilesetImage.BitmapSource.Size);

                if (rect.Contains(pt))
                {
                    pt.X = (pt.X / GlobalData.TileSize.Width) * GlobalData.TileSize.Width;
                    pt.Y = (pt.Y / GlobalData.TileSize.Height) * GlobalData.TileSize.Height;

                    this.tilesetSelection.Location = pt;
                    this.tilesetSelection.Size = GlobalData.TileSize;
                    this.IsSelectingTiles = true;
                    this.picTileset.Refresh();
                }
            }
        }

        private void picTileset_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.currentTexture != null)
            {
                Point pt = new Point(e.Location.X + this.TilesetOrigin.X, e.Location.Y + this.TilesetOrigin.Y);
                Rectangle rect = new Rectangle(Point.Empty, this.TilesetImage.BitmapSource.Size);

                if (rect.Contains(pt) && this.IsSelectingTiles && pt.X > this.TilesetSelection.Location.X && pt.Y > this.TilesetSelection.Location.Y)
                {
                    int tmpWidth = (int)((pt.X - this.TilesetSelection.X) / GlobalData.TileSize.Width) + 1;
                    int tmpHeight = (int)((pt.Y - this.TilesetSelection.Location.Y) / GlobalData.TileSize.Height) + 1;

                    this.tilesetSelection.Size = new Size(tmpWidth * GlobalData.TileSize.Width, tmpHeight * GlobalData.TileSize.Height);
                    this.picTileset.Refresh();

                }
            }
        }

        private void picTileset_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.currentTexture != null)
            {
                this.IsSelectingTiles = false;
                if (this.tilesetSelection.Size.Height + this.tilesetSelection.Location.Y > this.currentTexture.BitmapSource.Height)
                {
                    this.tilesetSelection.Height -= this.tilesetSelection.Size.Height + this.tilesetSelection.Location.Y - this.currentTexture.BitmapSource.Height;
                }
                this.currentTexture.SelectionLocation = this.TilesetSelection.Location;
                this.currentTexture.BitmapSelection = this.currentTexture.BitmapSource.Clone(this.tilesetSelection, PixelFormat.DontCare);
                this.RaiseTilesetSelectionChangedEvent(this.currentTexture);
            }
        }

        private void vPicTilesetScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            this.tilesetOrigin.Y = vPicTilesetScrollBar.Value;
            this.picTileset.Refresh();
        }

        private void hPicTilesetScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            this.tilesetOrigin.X = hPicTilesetScrollBar.Value;
            this.picTileset.Refresh();
        }

        private void picTileset_SizeChanged(object sender, EventArgs e)
        {
            this.RefreshScrollComponents(this.hPicTilesetScrollBar.Value, this.vPicTilesetScrollBar.Value);
        }

        private void comboTilesetFileSelecter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.textures.Count > this.comboTilesetFileSelecter.SelectedIndex)
            {
                this.currentTexture = this.textures[this.comboTilesetFileSelecter.SelectedIndex];
                this.RaiseTilesetChangedEvent(this.currentTexture);
                this.RefreshScrollComponents();
            }
        }

        private void toolStripButtonGrid_Click(object sender, EventArgs e)
        {
            this.IsGridActived = !this.IsGridActived;
        }

        private void RaiseTilesetChangedEvent(BitmapImageBundle texture)
        {
            if(this.TilesetChanged != null)
            {
                this.TilesetChanged(this, texture);
            }
        }

        private void RaiseTilesetSelectionChangedEvent(BitmapImageBundle texture)
        {
            if(this.TilesetSelectionChanged != null)
            {
                this.TilesetSelectionChanged(this, this.tilesetSelection);
            }
        }
        #endregion

        #region Methods
        private void LoadTilesetFileList()
        {
            if (Directory.Exists(GlobalData.TEXTURES_DIRECTORY_PATH))
            {
                string[] tilesetFiles = Directory.GetFiles(GlobalData.TEXTURES_DIRECTORY_PATH, "*.png", SearchOption.AllDirectories);
                foreach (string file in tilesetFiles)
                {
                    this.textures.Add(new BitmapImageBundle(file, Bitmap.FromFile(file) as Bitmap));
                    this.comboTilesetFileSelecter.Items.Add(file);
                }
            }
            else
            {
                Directory.CreateDirectory(GlobalData.TEXTURES_DIRECTORY_PATH);
            }
        }

        /// <summary>
        /// Met à jour les valeurs des scrollbars
        /// </summary>
        /// <param name="scrollX">Ancienne valeur de la scrollbar horizontale</param>
        /// <param name="scrollY">Ancienne valeur de la scrollbar verticale</param>
        private void RefreshScrollComponents(int scrollX = 0, int scrollY = 0)
        {
            if (this.currentTexture != null)
            {
                this.vPicTilesetScrollBar.Minimum = 0;
                this.vPicTilesetScrollBar.SmallChange = this.currentTexture.BitmapSource.Size.Height / 20;
                this.vPicTilesetScrollBar.LargeChange = this.currentTexture.BitmapSource.Size.Height / 5;
                int scrollHeightValue = this.currentTexture.BitmapSource.Size.Height + 50 - this.picTileset.Size.Height;
                this.vPicTilesetScrollBar.Maximum = scrollHeightValue > 0 ? scrollHeightValue : 1;
                this.vPicTilesetScrollBar.Maximum += this.vPicTilesetScrollBar.LargeChange;
                this.vPicTilesetScrollBar.Value = scrollY < this.vPicTilesetScrollBar.Maximum ? scrollY : this.vPicTilesetScrollBar.Maximum;

                this.hPicTilesetScrollBar.Minimum = 0;
                this.hPicTilesetScrollBar.SmallChange = this.currentTexture.BitmapSource.Size.Width / 20;
                this.hPicTilesetScrollBar.LargeChange = this.currentTexture.BitmapSource.Size.Width / 5;
                int scrollWidthValue = this.currentTexture.BitmapSource.Size.Width + 50 - this.picTileset.Size.Width;
                this.hPicTilesetScrollBar.Maximum = scrollWidthValue > 0 ? scrollWidthValue : 1;
                this.hPicTilesetScrollBar.Maximum += this.hPicTilesetScrollBar.LargeChange;
                this.hPicTilesetScrollBar.Value = scrollX < this.hPicTilesetScrollBar.Maximum ? scrollX : this.hPicTilesetScrollBar.Maximum;

                this.picTileset.Refresh();
            }
        }

        /// <summary>
        /// Dessine la grille si l'état donné est vrai
        /// </summary>
        /// <param name="state">L'état de dessin</param>
        /// <param name="e">Les données de dessin</param>
        private void DrawGrid(bool state, PaintEventArgs e)
        {
            if (!state) return;

            int tmpWidth = this.currentTexture.BitmapSource.Width / GlobalData.TileSize.Width;
            int tmpHeight = this.currentTexture.BitmapSource.Height / GlobalData.TileSize.Height + 1;

            for (int x = 0; x < tmpWidth + 1; x++)
                e.Graphics.DrawLine(this.GridColor,
                    x * GlobalData.TileSize.Width - this.tilesetOrigin.X,
                    -this.tilesetOrigin.Y,
                    x * GlobalData.TileSize.Width - this.tilesetOrigin.X,
                    tmpHeight * GlobalData.TileSize.Height - this.tilesetOrigin.Y);

            for (int y = 0; y < tmpHeight + 1; y++)
                e.Graphics.DrawLine(this.GridColor,
                    -this.tilesetOrigin.X,
                    y * GlobalData.TileSize.Height - this.tilesetOrigin.Y,
                    tmpWidth * GlobalData.TileSize.Width - this.tilesetOrigin.X,
                    y * GlobalData.TileSize.Height - this.tilesetOrigin.Y);
        }
        #endregion

        #region Properties
        public Pen CursorColor
        {
            get;
            set;
        }

        public bool IsSelectingTiles
        {
            get;
            private set;
        }

        public Point TilesetOrigin
        {
            get { return this.tilesetOrigin; }
        }

        public Rectangle TilesetSelection
        {
            get { return this.tilesetSelection; }
        }

        public BitmapImageBundle TilesetImage
        {
            get { return this.currentTexture; }
        }

        public bool IsGridActived
        {
            get { return this.isGridActived; }
            set
            {
                this.isGridActived = value;
                this.toolStripButtonGrid.Checked = this.IsGridActived;
                this.picTileset.Refresh();
            }
        }

        public Pen GridColor
        {
            get { return this.gridColor ?? Pens.Black; }
            set { this.gridColor = value; this.picTileset.Refresh(); }
        }
        #endregion
    }
}
