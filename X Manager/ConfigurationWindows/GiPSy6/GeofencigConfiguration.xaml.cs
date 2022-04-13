﻿using System;
using System.Collections.Generic;
using System.Device.Location;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Security.Principal;
using Microsoft.VisualBasic.FileIO;
using System.ComponentModel;
using System.Resources;
using System.Globalization;
//using System.Drawing;



namespace X_Manager.ConfigurationWindows
{
	/// <summary>
	/// Logica di interazione per GeofencigConfiguration.xaml
	/// </summary>
	/// 

	public class TileImage : Image
	{
		readonly string[] serverAr = { "a", "b", "c" };
		public SBitmap bIm;
		public int zoom, x, y;
		public int tiles;
		public int index;
		public Point p;
		GeofencigConfiguration parent;

		public TileImage(int index, int zoom, int x, int y, GeofencigConfiguration parent) : base()
		{
			this.zoom = zoom;
			this.x = x;
			this.y = y;
			this.parent = parent;
			this.index = index;
			Canvas.SetZIndex(this, 1);
			//bgw.WorkerSupportsCancellation = true;
			//bgw.DoWork += bgwWork;

			bIm = addTile(x, y, zoom);

		}

		public void redraw(ref Canvas canvas)
		{
			Canvas.SetLeft(this, p.X);
			Canvas.SetTop(this, p.Y);
			bIm = addTile(x, y, zoom);
			tiles = (int)Math.Pow(2, zoom);
		}

		public void move(double newX, double newY)
		{
			int newTileX = x;
			int newTileY = y;
			if (newX < -512)
			{
				newX += 1792;
				for (int i = 0; i < 7; i++)
				{
					newTileX++;
					if (newTileX == tiles)
					{
						newTileX = 0;
					}
				}
			}
			else if (newX > 1280)
			{
				newX -= 1792;
				for (int i = 0; i < 7; i++)
				{
					newTileX--;
					if (newTileX < 0)
					{
						newTileX = tiles - 1;
					}
				}
			}

			if (newY < -512)
			{
				newY += 1536;
				for (int i = 0; i < 6; i++)
				{
					newTileY++;
					if (newTileY == tiles)
					{
						newTileY = 0;
					}
				}

			}
			else if (newY > 1024)
			{
				newY -= 1536;
				for (int i = 0; i < 6; i++)
				{
					newTileY--;
					if (newTileY < 0)
					{
						newTileY = tiles - 1;
					}
				}
			}
			if ((newTileX != x) | (newTileY != y))
			{
				x = newTileX;
				y = newTileY;
				bIm = addTile(x, y, zoom);
				//try
				//{
				//	Source = Imaging.CreateBitmapSourceFromHBitmap(bIm.bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
				//}
				//catch (Exception ex)
				//{
				//	int a = 0;
				//}
			}

			Canvas.SetLeft(this, newX);
			Canvas.SetTop(this, newY);

		}

		public SBitmap addTile(int x, int y, int z)
		{
			string url;
			string filename = parent.ts.filename(x, y, zoom);
			SBitmap b = null;
			if (parent.bitnameAr.Contains(filename))
			{
				b = parent.sbitmapAr[parent.bitnameAr.IndexOf(filename)];
				updateSource(b);
			}
			else
			{
				url = parent.ts.url(x, y, zoom);
				b = new SBitmap();
				b.name = filename;
				b.url = url;

				Source = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.tile_background_loading.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

				var request = HttpWebRequest.Create(b.url);
				request.Timeout = 120;
				IAsyncResult asyncResult = request.BeginGetResponse(new AsyncCallback(cb), Tuple.Create(request, b));

			}

			return b;
		}

		private void cb(IAsyncResult res)
		{
			Tuple<WebRequest, SBitmap> reqBit = null;
			try
			{
				reqBit = (Tuple<WebRequest, SBitmap>)res.AsyncState;
				//var r = reqBit.Item1.GetResponse();
				Stream rs = reqBit.Item1.GetResponse().GetResponseStream();
				reqBit.Item2.bitmap = new System.Drawing.Bitmap(rs);
				Application.Current.Dispatcher.Invoke(() => updateSource(reqBit.Item2));
			}
			catch
			{
				reqBit.Item2.bitmap = new System.Drawing.Bitmap(1, 1);
				Application.Current.Dispatcher.Invoke(() => updateSource(reqBit.Item2));
			}

		}

		private void updateSource(SBitmap b)
		{
			//sviluppo
			//System.Drawing.Bitmap newBitmap = new System.Drawing.Bitmap(256, 256);
			//try
			//{
			//	using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(newBitmap))
			//	{
			//		graphics.DrawImage(b.bitmap, 0, 0);
			//		graphics.DrawRectangle(new System.Drawing.Pen(System.Drawing.Color.Black, 2), 0, 0, 256, 256);
			//		using (System.Drawing.Font arialFont = new System.Drawing.Font("Arial", 13))
			//		{
			//			string ondisk = "Remote";
			//			if (b.onDisk)
			//			{
			//				ondisk = "Local";
			//			}
			//			graphics.DrawString(ondisk, arialFont, System.Drawing.Brushes.Black, 10, 10);
			//		}
			//	}
			//}
			//catch (Exception ex)
			//{
			//	int bs = 0;
			//}



			lock (b)
			{
				if (b.bitmap.Width == 1)
				{
					b = new SBitmap(Properties.Resources.tile_background_offline);
				}
				Source = Imaging.CreateBitmapSourceFromHBitmap(b.bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
				Canvas.SetZIndex(this, 3);

				parent.bitnameAr.Add(b.name);
				parent.sbitmapAr.Add(b);
				if (parent.bitnameAr.Count > 1024)
				{
					parent.bitnameAr.RemoveAt(0);
					parent.sbitmapAr.RemoveAt(0);
				}
			}
			if (index == 32)
			{
				parent.rebuildMG();
			}
			if (index == 41)
			{
				parent.rendering = 0;
			}
		}
	}

	public class MapGrid : Grid
	{
		//public double[] A = new double[] { 200, 200 };
		//public double[] B = new double[] { 200, 200 };
		//public int x = 1000;
		//public int y = 1000;
		public Point A = new Point(-218, 0);
		public Point B = new Point(-218, 0);

		public int index = -1;

		GeofencigConfiguration parent;
		Canvas canvas;
		//Point startPoint;

		public bool inside = false;
		public MapGrid(int index, GeofencigConfiguration parent) : base()
		{
			Width = 50;
			Height = 50;
			Background = new SolidColorBrush(Color.FromArgb(0x80, 0, 0, 0));
			this.parent = parent;
			this.index = index;
			canvas = parent.canvas;
			Canvas.SetZIndex(this, 4);
		}

		public MapGrid(GeofencigConfiguration.Square sq, int index, GeofencigConfiguration parent) : base()
		{
			Width = 50;
			Height = 50;
			Background = new SolidColorBrush(Color.FromArgb(0x80, 0, 0, 0));
			this.parent = parent;
			this.index = index;
			canvas = parent.canvas;
			Canvas.SetZIndex(this, 4);
			A.X = sq.X1;
			A.Y = sq.Y1;
			B.X = sq.X2;
			B.Y = sq.Y2;
		}

		public void updateBounds()
		{
			A = parent.relativePixel2Coordinates(new Point(Canvas.GetLeft(this), Canvas.GetTop(this)));
			B = parent.relativePixel2Coordinates(new Point(Canvas.GetLeft(this) + Width, Canvas.GetTop(this) + Height));
		}

		public bool placeOnMap()
		{

			if (A.X < -200) return false;

			Point offset = parent.relativePixel2AbsolutePixel(new Point(0, 0));

			Point start = parent.coordinates2TileXYDecimal(A);
			Point stop = parent.coordinates2TileXYDecimal(B);

			start.X = start.X * 256 - offset.X;
			start.Y = start.Y * 256 - offset.Y;
			stop.X = stop.X * 256 - offset.X;
			stop.Y = stop.Y * 256 - offset.Y;

			int c = parent.tiles * 256;
			if (start.X - 768 < -c)
			{
				start.X += c;
			}
			else if (stop.X > c)
			{
				stop.X -= c;
			}

			if (start.Y - 580 < -c)
			{
				start.Y += c;
			}
			else if (stop.Y > c)
			{
				stop.Y -= c;
			}

			if (stop.X < start.X)
			{
				stop.X += c;
			}

			if (stop.Y < start.Y)
			{
				stop.Y += c;
			}

			Width = stop.X - start.X;
			Height = stop.Y - start.Y;
			Canvas.SetLeft(this, start.X);
			Canvas.SetTop(this, start.Y);

			if ((start.X + Width >= -512) | (start.X < 1024) | (start.Y + Height >= -512) | (start.Y < 768))
			{
				if (!canvas.Children.Contains(this))
				{
					parent.canvas.Children.Add(this);
					MouseLeftButtonDown -= parent.sClick;
					MouseLeftButtonDown += parent.sClick;
				}
			}

			return true;
		}

		public void move(Point p)
		{
			if (A.X < -200) return;

			var yb = p.Y + Canvas.GetTop(this);

			p.X += Canvas.GetLeft(this);
			p.Y += Canvas.GetTop(this);
			if ((p.X + Width < -512) | (p.X > 1023) | (p.Y + Height < -512) | (p.Y > 767))
			{
				canvas.Children.Remove(this);
				MouseLeftButtonDown -= parent.sClick;
				inside = false;
			}
			else
			{
				inside = true;
				if (!canvas.Children.Contains(this))
				{
					canvas.Children.Add(this);
					MouseLeftButtonDown -= parent.sClick;
					MouseLeftButtonDown += parent.sClick;
				}
			}
			int c = parent.tiles * 256;
			if (p.Y - 580 < -c)
			{
				p.Y += c;
			}
			else if (p.Y + Height > c)
			{
				p.Y -= c;
			}

			if (p.X - 768 < -c)
			{
				p.X += c;
			}
			else if (p.X + Width > c)
			{
				p.X -= c;
			}
			Canvas.SetLeft(this, p.X);
			Canvas.SetTop(this, p.Y);
		}

		public void catchEllipses()
		{
			if (canvas.Children.Contains(parent.ellAr[0]))
			{
				for (int j = 0; j < 8; j++)
				{
					canvas.Children.Remove(parent.ellAr[j]);
				}
			}

			int newX = (int)Canvas.GetLeft(this) - 6;
			int newY = (int)Canvas.GetTop(this) - 6;
			int h = (int)Height;
			int w = (int)Width;

			Canvas.SetLeft(parent.ellAr[0], newX);
			Canvas.SetLeft(parent.ellAr[3], newX);
			Canvas.SetLeft(parent.ellAr[5], newX);
			Canvas.SetLeft(parent.ellAr[2], newX + w);
			Canvas.SetLeft(parent.ellAr[4], newX + w);
			Canvas.SetLeft(parent.ellAr[7], newX + w);
			Canvas.SetLeft(parent.ellAr[1], newX + w / 2);
			Canvas.SetLeft(parent.ellAr[6], newX + w / 2);

			Canvas.SetTop(parent.ellAr[0], newY);
			Canvas.SetTop(parent.ellAr[1], newY);
			Canvas.SetTop(parent.ellAr[2], newY);
			Canvas.SetTop(parent.ellAr[5], newY + h);
			Canvas.SetTop(parent.ellAr[6], newY + h);
			Canvas.SetTop(parent.ellAr[7], newY + h);
			Canvas.SetTop(parent.ellAr[3], newY + h / 2);
			Canvas.SetTop(parent.ellAr[4], newY + h / 2);

			for (int j = 0; j < 8; j++)
			{
				canvas.Children.Add(parent.ellAr[j]);
			}
		}

	}

	public class SBitmap : IDisposable
	{
		public System.Drawing.Bitmap bitmap;
		public string name;
		public string url;
		//sviluppo
		public bool onDisk = false;
		///sviluppo

		public SBitmap(string filename)
		{
			bitmap = new System.Drawing.Bitmap(filename);
			name = System.IO.Path.GetFileName(filename);
		}

		public SBitmap(System.Drawing.Bitmap bitmap)
		{
			this.bitmap = new System.Drawing.Bitmap(bitmap);
			name = "offline";
		}

		public SBitmap()
		{

		}
		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}
	}
	public class OsmTileServer : TileServer
	{
		static int counter = 0;

		public override string url(int x, int y, int z)
		{
			string s = "https://";
			s += Encoding.ASCII.GetString(new byte[] { (byte)(97 + counter) });
			counter++;
			if (counter == 3) counter = 0;
			s += ".tile.openstreetmap.org/" + z.ToString();
			s += "/" + x.ToString();
			s += "/" + y.ToString();
			s += ".png";

			return s;
		}

		public override string filename(int x, int y, int z)
		{
			string s = x.ToString() + "_" + y.ToString() + "_" + z.ToString() + ".png";
			return s;
		}
	}
	public class ThunderTileServer : TileServer
	{
		public override string url(int x, int y, int z)
		{
			string s = "https://tile.thunderforest.com/outdoors/";
			s += z.ToString() + "/" + x.ToString() + "/" + y.ToString();
			s += ".png?apikey=f3c19c794dc04642a22a1c2ebbe85c0c";

			return s;
		}

		public override string filename(int x, int y, int z)
		{
			string s = x.ToString() + "_" + y.ToString() + "_" + z.ToString() + ".png";
			return s;
		}

	}
	public abstract class TileServer
	{
		public abstract string url(int x, int y, int z);
		public abstract string filename(int x, int y, int z);
	}
	partial class GeofencigConfiguration : PageCopy
	{
		[DllImport("wininet.dll")]
		private extern static bool InternetGetConnectedState(out int description, int reservedValue);

		//const double casaLat = 41.795110;
		//const double casaLon = 12.485203;
		Point casa = new Point(12.485203, 41.795110);

		private Point mousePosition;
		bool moving = false;
		public volatile int rendering = 0;
		int xMin;
		public int tiles;
		bool locked = false;
		public Ellipse e1 = new Ellipse();
		public Ellipse e2 = new Ellipse();
		public Ellipse e3 = new Ellipse();
		public Ellipse e4 = new Ellipse();
		public Ellipse e5 = new Ellipse();
		public Ellipse e6 = new Ellipse();
		public Ellipse e7 = new Ellipse();
		public Ellipse e8 = new Ellipse();
		bool isCircle = false;
		bool isSquare = false;
		int circleIndex;
		int zoom = 6;

		bool conn;
		int index;

		public double boundYN;
		public double boundYS;

		public Point boundA = new Point(200, 200);
		public Point boundB = new Point(200, 200);

		public string home;

		public TileImage[] iAr = new TileImage[42];
		public TileServer ts;
		List<TimePanel> timePanelAr;
		StackPanel[] spAr;
		CheckBox[] cbAr;
		ComboBox[] cbxAr;
		CheckBox[] ocCbAr;
		TextBox[] oxaAr;
		TextBox[] oyaAr;
		TextBox[] oxbAr;
		TextBox[] oybAr;
		TextBox[] tbArr;
		MapGrid[] mgAr = new MapGrid[10];
		MapGrid actMapGrid;
		public Ellipse[] ellAr;
		//public WebClient webClient;
		//public TileDataTable tileDb;
		public List<SBitmap> sbitmapAr;
		public List<string> bitnameAr;
		public Image fakeZoom;
		byte[] conf;
		bool loaded = false;
		uint[] sch;
		int[] oldCbVal = new int[2];
		//string appDataPath;
		public struct Square
		{
			public double X1;
			public double Y1;
			public double X2;
			public double Y2;
		}
		public Square[] squareAr;

		public GeofencigConfiguration(byte[] conf, int index, GiPSy6ConfigurationMain parent)
		{
			InitializeComponent();

			//appDataPath = parent.appDataPath;
			sch = new uint[2];
			sbitmapAr = parent.sbitmapAr;
			bitnameAr = parent.bitnameAr;
			conn = parent.conn;
			this.index = index;
			spAr = new StackPanel[10] { sp0, sp1, sp2, sp3, sp4, sp5, sp6, sp7, sp8, sp9 };
			cbAr = new CheckBox[10] { cb0, cb1, cb2, cb3, cb4, cb5, cb6, cb7, cb8, cb9 };
			ocCbAr = new CheckBox[10] { ocCB0, ocCB1, ocCB2, ocCB3, ocCB4, ocCB5, ocCB6, ocCB7, ocCB8, ocCB9 };
			oxaAr = new TextBox[10] { ocxa0, ocxa1, ocxa2, ocxa3, ocxa4, ocxa5, ocxa6, ocxa7, ocxa8, ocxa9 };
			oyaAr = new TextBox[10] { ocya0, ocya1, ocya2, ocya3, ocya4, ocya5, ocya6, ocya7, ocya8, ocya9 };
			oxbAr = new TextBox[10] { ocxb0, ocxb1, ocxb2, ocxb3, ocxb4, ocxb5, ocxb6, ocxb7, ocxb8, ocxb9 };
			oybAr = new TextBox[10] { ocyb0, ocyb1, ocyb2, ocyb3, ocyb4, ocyb5, ocyb6, ocyb7, ocyb8, ocyb9 };
			cbxAr = new ComboBox[2] { eTimeUnitCB, fTimeUnitCB };
			tbArr = new TextBox[] { eValTB, fValTB };
			actMapGrid = null;
			timePanelAr = new List<TimePanel>();
			ellAr = new Ellipse[8] { e1, e2, e3, e4, e5, e6, e7, e8 };
			for (int i = 0; i < 8; i++)
			{
				ellAr[i].Width = ellAr[i].Height = 12;
				ellAr[i].Fill = new SolidColorBrush(Color.FromArgb(0x80, 0xff, 0, 0));
				Panel.SetZIndex(ellAr[i], 4);
			}

			this.conf = conf;

			//ts = new osmTileServer();
			ts = new ThunderTileServer();

			squareAr = new Square[10];
			int squareOffset = 128;
			int scheduleOffset = 288;
			string sA = "E:";
			string sB = "F:";
			if (index == 2)
			{
				sA = "G:";
				sB = "H:";
				schAL.Text = "Schedule G:";
				schBL.Text = "Schedule H:";
				titleL.Text = " 4 - Geofencing 2";
				squareOffset += 192;
				scheduleOffset += 192;
			}
			sch[0] = ((uint)conf[scheduleOffset] << 24) + ((uint)conf[scheduleOffset + 1] << 16) + ((uint)conf[scheduleOffset + 2] << 8) + conf[scheduleOffset + 3];
			sch[1] = ((uint)conf[scheduleOffset + 4] << 24) + ((uint)conf[scheduleOffset + 5] << 16) + ((uint)conf[scheduleOffset + 6] << 8) + conf[scheduleOffset + 7];

			bool schedBselected = false;
			scheduleOffset += 8;
			for (int i = 0; i < 24; i++)
			{
				TimePanel timePanel = new TimePanel(sA, sB);
				timePanel.time = i;
				Grid.SetRow(timePanel, i);
				timeGrid.Children.Add(timePanel);
				timePanelAr.Add(timePanel);
				if (conf[scheduleOffset + i] == 0)
				{
					timePanel.isChecked = false;
				}
				else if (conf[scheduleOffset + i] == 2)
				{
					schedBselected = true;
					timePanel.setAB();
					timePanel.sel = 1;
				}
				if (schedBselected)
				{
					foreach (TimePanel t in timePanelAr)
					{
						t.setAB();
					}
				}
				timePanel.unChecked += tpUnchecked;

				if (i < 10)
				{
					for (int j = 0; j < 4; j++)
					{
						squareAr[i].X1 *= 256;
						squareAr[i].X1 += conf[squareOffset + j];
						squareAr[i].Y1 *= 256;
						squareAr[i].Y1 += conf[squareOffset + 4 + j];

						squareAr[i].X2 *= 256;
						squareAr[i].X2 += conf[squareOffset + 8 + j];
						squareAr[i].Y2 *= 256;
						squareAr[i].Y2 += conf[squareOffset + 12 + j];
					}
					squareOffset += 16;

					if (squareAr[i].X1 > 0x7FFFFFFF) squareAr[i].X1 -= 0x100000000;
					if (squareAr[i].Y1 > 0x7FFFFFFF) squareAr[i].Y1 -= 0x100000000;
					if (squareAr[i].X2 > 0x7FFFFFFF) squareAr[i].X2 -= 0x100000000;
					if (squareAr[i].Y2 > 0x7FFFFFFF) squareAr[i].Y2 -= 0x100000000;

					squareAr[i].X1 /= 10000000;
					squareAr[i].Y1 /= 10000000;
					squareAr[i].X2 /= 10000000;
					squareAr[i].Y2 /= 10000000;

					cbAr[i].Checked += cbChecked;
					cbAr[i].Unchecked += cbUnchecked;
					mgAr[i] = new MapGrid(squareAr[i], i, this);
					//mgAr[i].placeOnMap();
				}
			}

			fakeZoom = new Image();

		}

		private void PageCopy_Loaded(object sender, RoutedEventArgs e)
		{
			if (!loaded)
			{
				loaded = true;
			}
			else
			{
				return;
			}

			if (conn)
			{
				canvas.Children.Remove(offlineGrid);

				GeoCoordinateWatcher watcher = new GeoCoordinateWatcher();
				try
				{
					watcher.TryStart(false, TimeSpan.FromSeconds(4));
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
				}

				Point pos = new Point(12, 41);
				if (!watcher.Position.Location.IsUnknown)
				{
					pos.X = watcher.Position.Location.Longitude;
					pos.Y = watcher.Position.Location.Latitude;
				}

				Point tl = new Point(180, -58.011);
				Point br = new Point(-180, 58.011);

				for (int i = 0; i < 10; i++)
				{
					if (squareAr[i].X1 > -200)
					{
						if (squareAr[i].X1 < tl.X) tl.X = squareAr[i].X1;
						if (squareAr[i].Y1 > tl.Y) tl.Y = squareAr[i].Y1;
						if (squareAr[i].X2 > br.X) br.X = squareAr[i].X2;
						if (squareAr[i].Y2 < br.Y) br.Y = squareAr[i].Y2;
					}
					else
					{
						continue;
					}
				}

				if (tl.X != 180)
				{
					pos.X = ((br.X - tl.X) / 2) + tl.X;
					pos.Y = ((tl.Y - br.Y) / 2) + br.Y;

					for (int i = 19; i > 2; i--)
					{
						zoom = i;
						Point p1 = coordinates2AbsolutePixel(br);
						Point p2 = coordinates2AbsolutePixel(tl);
						if ((p1.X - p2.X) < 768)
						{
							if ((p1.Y - p2.Y) < 580)
							{
								break;
							}
						}
					}
				}

				Panel.SetZIndex(fakeZoom, 2);
				canvas.Children.Add(fakeZoom);

				rendering = 1;

				fillCanvas(pos);

				pos = coordinates2TileXYDecimal(pos);
				pos.X = 128 - (int)((pos.X - Math.Truncate(pos.X)) * 256);
				pos.Y = 256 - (int)((pos.Y - Math.Truncate(pos.Y)) * 256) + 34;
				mousePosition = new Point(0, 0);
				moving = true;
				moveMap(pos);
				moving = false;

				lockB.Content = "UNLOCKED\r\npress to lock";
				lockB.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0xee, 0));
				locked = false;

				for (int i = 0; i < 8; i++)
				{
					ellAr[i].MouseLeftButtonDown += eClick;
				}

				boundA = relativePixel2Coordinates(new Point(-511, -511));
				boundB = relativePixel2Coordinates(new Point(1279, 1023));

			}
			else
			{
				for (int i = 0; i < 10; i++)
				{
					oxaAr[i].Text = squareAr[i].X1.ToString();
					oxaAr[i].KeyDown += tbKd;
					oxaAr[i].LostFocus += tbLf;
					oxaAr[i].GotFocus += tbGf;
					oyaAr[i].Text = squareAr[i].Y1.ToString();
					oyaAr[i].KeyDown += tbKd;
					oyaAr[i].LostFocus += tbLf;
					oyaAr[i].GotFocus += tbGf;
					oxbAr[i].Text = squareAr[i].X2.ToString();
					oxbAr[i].KeyDown += tbKd;
					oxbAr[i].LostFocus += tbLf;
					oxbAr[i].GotFocus += tbGf;
					oybAr[i].Text = squareAr[i].Y2.ToString();
					oybAr[i].KeyDown += tbKd;
					oybAr[i].LostFocus += tbLf;
					oybAr[i].GotFocus += tbGf;
					ocCbAr[i].Checked += cbCh;
					ocCbAr[i].Unchecked += cbunCh;
					ocCbAr[i].IsChecked = !(squareAr[i].X1 > 200);
				}
			}
			for (int i = 0; i < 2; i++)
			{
				if ((sch[i] % 3600) == 0)
				{
					sch[i] /= 3600;
					cbxAr[i].SelectedIndex = 2;
				}
				else if ((sch[i] % 60) == 0)
				{
					sch[i] /= 60;
					cbxAr[i].SelectedIndex = 1;
				}
				else
				{
					cbxAr[i].SelectedIndex = 0;
				}
				oldCbVal[i] = cbxAr[i].SelectedIndex;
				tbArr[i].Text = sch[i].ToString();
				tbArr[i].LostFocus += validate;
				tbArr[i].KeyDown += validate;
				cbxAr[i].SelectionChanged += cbSelChanged;
			}

			eValTB.Text = sch[0].ToString();
			fValTB.Text = sch[1].ToString();
			int c = 512;
			if (index == 2) c = 513;
			if (conf[c] == 1) mainEnableCB.IsChecked = true;
			mainEnableCB.Checked += enableChecked;
		}

		private void validate(object sender, RoutedEventArgs e)
		{
			TextBox tb = (TextBox)sender;
			validate(ref tb);
		}

		private void validate(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				TextBox tb = (TextBox)sender;
				validate(ref tb);

				return;
			}
			int ascii = KeyInterop.VirtualKeyFromKey(e.Key);
			if (((ascii < 48) | (ascii > 57)) && ((ascii < 96) | (ascii > 105)))
			{
				if (ascii != 9)
				{
					e.Handled = true;
				}
			}
		}

		private void validate(ref TextBox tb)
		{
			int ind = Array.IndexOf(tbArr, tb);
			//for (ind = 0; ind < 2; ind++)
			//{
			//	if (tb == tbArr[ind]) break;
			//}

			cbxAr[ind].SelectionChanged -= cbSelChanged;

			uint newVal = 0;
			uint.TryParse(tbArr[ind].Text, out newVal);
			try
			{
				if (cbxAr[ind].SelectedIndex == 2)
				{
					newVal *= 3600;
				}
				else if (cbxAr[ind].SelectedIndex == 1)
				{
					newVal *= 60;
				}
			}
			catch
			{
				newVal = 0;
			}
			if (newVal == 0)
			{
				tbArr[ind].Text = sch[ind].ToString();
				return;
			}
			else
			{
				if ((newVal % 3600) == 0)
				{
					cbxAr[ind].SelectedIndex = 2;
					newVal /= 3600;
				}
				else if ((newVal % 60) == 0)
				{
					cbxAr[ind].SelectedIndex = 1;
					newVal /= 60;
				}
				else
				{
					cbxAr[ind].SelectedIndex = 0;
				}
			}

			sch[ind] = newVal;
			tbArr[ind].Text = sch[ind].ToString();
			oldCbVal[ind] = cbxAr[ind].SelectedIndex;
			cbxAr[ind].SelectionChanged += cbSelChanged;
		}

		private void tpUnchecked(object sender, EventArgs e)
		{
			bool atLeastOne = false;
			foreach (TimePanel t in timePanelAr)
			{
				if (t.isChecked)
				{
					atLeastOne = true;
					break;
				}
			}
			if (!atLeastOne)
			{
				if (mainEnableCB.IsChecked == true)
				{
					mainEnableCB.IsChecked = false;
				}
			}
		}

		private void cbSelChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBox cb = (ComboBox)sender;

			int ind;
			for (ind = 0; ind < 4; ind++)
			{
				if (cb == cbxAr[ind]) break;
			}

			if (cbxAr[ind].SelectedIndex == oldCbVal[ind]) return;

			if (cbxAr[ind].SelectedIndex == (oldCbVal[ind] - 2))
			{
				sch[ind] *= 3600;

			}
			else if (cbxAr[ind].SelectedIndex == (oldCbVal[ind] - 1))
			{
				sch[ind] *= 60;
			}
			else if (cbxAr[ind].SelectedIndex == (oldCbVal[ind] + 1))
			{
				sch[ind] /= 60;
			}
			else if (cbxAr[ind].SelectedIndex == (oldCbVal[ind] + 2))
			{
				sch[ind] /= 3600;
			}

			if (sch[ind] == 0) sch[ind] = 1;

			oldCbVal[ind] = cbxAr[ind].SelectedIndex;
			tbArr[ind].Text = sch[ind].ToString();
		}

		private void cbunCh(object sender, EventArgs e)
		{
			int i = Array.IndexOf(ocCbAr, (CheckBox)sender);
			oxaAr[i].Visibility = Visibility.Hidden;
			oyaAr[i].Visibility = Visibility.Hidden;
			oxbAr[i].Visibility = Visibility.Hidden;
			oybAr[i].Visibility = Visibility.Hidden;

		}

		private void cbCh(object sender, EventArgs e)
		{
			int i = Array.IndexOf(ocCbAr, (CheckBox)sender);
			if (squareAr[i].X1 > 200)
			{
				oxaAr[i].Text = "1";
				squareAr[i].X1 = 1;
				oyaAr[i].Text = "1";
				squareAr[i].Y1 = 1;
				oxbAr[i].Text = "-1";
				squareAr[i].X2 = -1;
				oybAr[i].Text = "-1";
				squareAr[i].Y2 = -1;
			}
			else
			{
				oxaAr[i].Text = squareAr[i].X1.ToString();
				oyaAr[i].Text = squareAr[i].Y1.ToString();
				oxbAr[i].Text = squareAr[i].X2.ToString();
				oybAr[i].Text = squareAr[i].Y2.ToString();
			}

			oxaAr[i].Visibility = Visibility.Visible;
			oyaAr[i].Visibility = Visibility.Visible;
			oxbAr[i].Visibility = Visibility.Visible;
			oybAr[i].Visibility = Visibility.Visible;
		}

		private void tbLf(object sender, EventArgs e)
		{
			tbValidate((TextBox)sender);
		}

		private void tbKd(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				tbValidate((TextBox)sender);
			}
		}

		private void tbGf(object sender, EventArgs e)
		{
			((TextBox)sender).SelectAll();
		}

		private void tbValidate(TextBox tb)
		{
			int ind = 0;
			int vert = 0;
			TextBox[] tbAr = null;
			Square sq;
			List<TextBox[]> tbList = new List<TextBox[]> { oxaAr, oyaAr, oxbAr, oybAr };
			for (int i = 0; i < 4; i++)
			{
				tbAr = tbList[i];
				ind = Array.IndexOf(tbAr, tb);
				if (ind >= 0)
				{
					sq = squareAr[ind];
					vert = i;
					break;
				}
			}
			double newVal;
			tbAr[ind].Text = tbAr[ind].Text.Replace(",", ".");
			bool convOk = double.TryParse(tbAr[ind].Text, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out newVal);
			if (vert == 0)  //TopLeft Long
			{
				if (!convOk)
				{
					tbAr[ind].Text = squareAr[ind].X1.ToString();
					return;
				}
				if ((newVal >= 180) | (newVal <= -180))
				{
					tbAr[ind].Text = squareAr[ind].X1.ToString();
					return;
				}
				squareAr[ind].X1 = newVal;
			}
			else if (vert == 1)
			{
				if (!convOk)
				{
					tbAr[ind].Text = squareAr[ind].Y1.ToString();
					return;
				}
				if ((newVal >= 85.0511) | (newVal <= -85.0511))
				{
					tbAr[ind].Text = squareAr[ind].Y1.ToString();
					return;
				}
				squareAr[ind].Y1 = newVal;
			}
			else if (vert == 2)
			{
				if (!convOk)
				{
					tbAr[ind].Text = squareAr[ind].X2.ToString();
					return;
				}
				if ((newVal >= 180) | (newVal <= -180))
				{
					tbAr[ind].Text = squareAr[ind].X2.ToString();
					return;
				}
				squareAr[ind].X2 = newVal;
			}
			else if (vert == 3)
			{
				if (!convOk)
				{
					tbAr[ind].Text = squareAr[ind].Y2.ToString();
					return;
				}
				if ((newVal >= 85.0511) | (newVal <= -85.0511))
				{
					tbAr[ind].Text = squareAr[ind].Y2.ToString();
					return;
				}
				squareAr[ind].Y2 = newVal;
			}
		}

		private void lockBClick(object sender, RoutedEventArgs e)
		{
			//sviluppo
			MessageBox.Show(zoom.ToString());
			///sviluppo
			if (locked)
			{
				lockB.Content = "UNLOCKED\r\npress to lock";
				lockB.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0xee, 0));
				locked = false;
				toggleRecs(false);
			}
			else
			{
				if (canvas.Children.Contains(ellAr[0]))
				{
					for (int i = 0; i < 8; i++)
					{
						canvas.Children.Remove(ellAr[i]);
					}
				}
				lockB.Content = "LOCKED\r\npress to unlock";
				lockB.Foreground = new SolidColorBrush(Color.FromArgb(255, 0xEE, 0x0, 0x0));
				locked = true;
				toggleRecs(true);
			}
		}

		private void eClick(object sender, MouseButtonEventArgs e)
		{
			isSquare = false;
			isCircle = true;
			circleIndex = Array.IndexOf(ellAr, (Ellipse)sender);
			//if ((Ellipse)sender == ex)
			//{
			//isCircleX = true;
			//	isCircleY = isSquare = false;
			//}
			//else
			//{
			//	isCircleY = true;
			//	isCircleX = isSquare = false;
			//}
		}

		public void sClick(object sender, MouseButtonEventArgs e)
		{
			isSquare = true;
			isCircle = false;

			int index = 1000;
			for (int i = 0; i < 10; i++)
			{
				if ((MapGrid)sender == mgAr[i])
				{
					index = i;
					break;
				}
			}
			if (index < 10)
			{
				spClick(new Tuple<StackPanel, int>(spAr[index], 0), e);
			}

		}

		private void CanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (rendering > 0)
			{
				return;
			}

			if (canvas.CaptureMouse())
			{
				moving = true;
				mousePosition = e.GetPosition(canvas);
			}
		}

		private void CanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			canvas.ReleaseMouseCapture();
			moving = false;
			if (isCircle | isSquare)
			{
				if (isSquare)
				{
					var p1 = relativePixel2AbsolutePixel(new Point(0, Canvas.GetTop(actMapGrid))).Y;
					var p2 = relativePixel2AbsolutePixel(new Point(0, Canvas.GetTop(actMapGrid) + actMapGrid.Height)).Y;
					if (p2 < p1)
					{
						//var delta = p1 - p2;
						canvas.Children.Remove(actMapGrid);
						var p = new Point(Canvas.GetLeft(actMapGrid), Canvas.GetTop(actMapGrid));

						actMapGrid.A.X = relativePixel2Coordinates(p).X;
						actMapGrid.A.Y = 85.03;

						p.X += actMapGrid.Width;
						p.Y = (coordinates2RelativePixel(actMapGrid.A).Y) + actMapGrid.Height;
						actMapGrid.B = relativePixel2Coordinates(p);

						actMapGrid.placeOnMap();
						actMapGrid.catchEllipses();
					}
				}
				else if (isCircle)
				{
					var y1 = relativePixel2AbsolutePixel(new Point(0, Canvas.GetTop(actMapGrid))).Y;
					var y2 = relativePixel2AbsolutePixel(new Point(0, Canvas.GetTop(actMapGrid) + actMapGrid.Height)).Y;

					if (y1 > y2)
					{
						var newHeight = ((tiles * 256) - 1) - y1;
						if (newHeight < 5)
						{
							y1 -= 5;
							newHeight = ((tiles * 256) - 1) - y1;
						}
						y2 = tiles * 256 - 1;
						actMapGrid.A.Y = absolutePixel2Coordinates(new Point(0, y1)).Y;
						actMapGrid.B.Y = absolutePixel2Coordinates(new Point(0, y2)).Y;
						canvas.Children.Remove(actMapGrid);
						actMapGrid.placeOnMap();
						actMapGrid.catchEllipses();
					}
				}
				actMapGrid.updateBounds();
				isCircle = isSquare = false;
			}
		}

		private void CanvasMouseMove(object sender, MouseEventArgs e)
		{
			moveMap(e.GetPosition(canvas));
		}

		private void moveMap(Point position)
		{
			if (moving)
			{
				//var position = e.GetPosition(canvas);
				var offset = position - mousePosition;
				mousePosition = position;

				if (locked)
				{
					isSquare = isCircle = false;
				}

				if (isSquare)
				{
					double newX = Canvas.GetLeft(actMapGrid) + offset.X;
					double newY = Canvas.GetTop(actMapGrid) + offset.Y;

					Canvas.SetLeft(actMapGrid, newX);
					Canvas.SetTop(actMapGrid, newY);
					for (int i = 0; i < 8; i++)
					{
						Canvas.SetLeft(ellAr[i], Canvas.GetLeft(ellAr[i]) + offset.X);
						Canvas.SetTop(ellAr[i], Canvas.GetTop(ellAr[i]) + offset.Y);
					}
				}
				else if (isCircle)
				{
					double newX = Canvas.GetLeft(ellAr[circleIndex]) + offset.X;
					double newY = Canvas.GetTop(ellAr[circleIndex]) + offset.Y;
					if (circleIndex == 0 | circleIndex == 3 | circleIndex == 5)
					{
						if (newX + 24 >= Canvas.GetLeft(ellAr[2]))
						{
							return;
						}
						Canvas.SetLeft(actMapGrid, newX + 6);
						actMapGrid.Width -= offset.X;
					}
					if (circleIndex == 0 | circleIndex == 1 | circleIndex == 2)
					{
						if (newY + 24 > Canvas.GetTop(ellAr[6]))
						{
							return;
						}
						Canvas.SetTop(actMapGrid, newY + 6);
						actMapGrid.Height -= offset.Y;
					}
					if (circleIndex == 2 | circleIndex == 4 | circleIndex == 7)
					{
						if (newX - 24 < Canvas.GetLeft(ellAr[0]))
						{
							return;
						}
						actMapGrid.Width += offset.X;
					}
					if (circleIndex == 7 | circleIndex == 6 | circleIndex == 5)
					{
						actMapGrid.Height += offset.Y;
					}
					newX = Canvas.GetLeft(actMapGrid) - 6;
					newY = Canvas.GetTop(actMapGrid) - 6;
					int h = (int)actMapGrid.Height;
					int w = (int)actMapGrid.Width;

					Canvas.SetLeft(ellAr[0], newX);
					Canvas.SetLeft(ellAr[3], newX);
					Canvas.SetLeft(ellAr[5], newX);
					Canvas.SetLeft(ellAr[2], newX + w);
					Canvas.SetLeft(ellAr[4], newX + w);
					Canvas.SetLeft(ellAr[7], newX + w);
					Canvas.SetLeft(ellAr[1], newX + w / 2);
					Canvas.SetLeft(ellAr[6], newX + w / 2);

					Canvas.SetTop(ellAr[0], newY);
					Canvas.SetTop(ellAr[1], newY);
					Canvas.SetTop(ellAr[2], newY);
					Canvas.SetTop(ellAr[5], newY + h);
					Canvas.SetTop(ellAr[6], newY + h);
					Canvas.SetTop(ellAr[7], newY + h);
					Canvas.SetTop(ellAr[3], newY + h / 2);
					Canvas.SetTop(ellAr[4], newY + h / 2);

				}
				else
				{
					bool moveEllipses = canvas.Children.Contains(ellAr[0]);
					var c = tiles * 256;
					for (int i = 0; i < 42; i++)
					{
						double newX = Canvas.GetLeft(iAr[i]) + offset.X;
						double newY = Canvas.GetTop(iAr[i]) + offset.Y;
						iAr[i].move(newX, newY);
						if (i < 10)
						{
							mgAr[i].move((Point)offset);
							if ((i < 8) & moveEllipses)
							{
								newX = Canvas.GetLeft(ellAr[i]) + offset.X;
								newY = Canvas.GetTop(ellAr[i]) + offset.Y;
								if (newX < -512)
								{
									newX += c;
								}
								else if (newX > 1280)
								{
									newX -= c;
								}
								if (newY < -512)
								{
									newY += c;

								}
								else if (newY > 1024)
								{
									newY -= c;
								}
								Canvas.SetLeft(ellAr[i], newX);
								Canvas.SetTop(ellAr[i], newY);
							}
						}
					}
				}
			}
		}

		private void CanvasMouseWheel(object sender, MouseWheelEventArgs e)
		{
			//Rimuove la gestione evento rotella del mouse
			if (rendering > 0)
			{
				return;
			}

			//recupera il punto in cui è stata mossa la rotella e calcola la distanza dal centro della mappa
			Point p = e.GetPosition(canvas);
			Point fromCenter = new Point(p.X - 384, p.Y - 290);
			Point zPos;

			if (e.Delta > 0)    //Ingrandimento
			{
				if (zoom == 19) //Non va oltre lo zoom 19
				{
					return;
				}
				fakeZoom.Width = 1536;  //Nuove dimensioni della fakemap
				fakeZoom.Height = 1160;

				zPos = new Point(-384 - fromCenter.X, -290 - 160 - fromCenter.Y);   //Nuova posizione della fakemap
			}
			else    //Riduzione
			{
				if (zoom == 3)  //No va oltre lo zoom 3
				{
					return;
				}
				fakeZoom.Width = 384;   //Nuove dimensioni della fakemap
				fakeZoom.Height = 290;

				zPos = new Point(192 + (fromCenter.X / 2), 145 - 40 + (fromCenter.Y / 2));  //Nuova posizione della fakemap
			}

			//Rimuove eventuali rettangoli e pallini per ridimensionamento rettangoli
			removeMG();

			//Crea uno screenshot della mappa e la inserisce come immagine fakezoom
			RenderTargetBitmap rtb = new RenderTargetBitmap(768, 580, 96, 96, PixelFormats.Pbgra32);
			rtb.Render(canvas);
			fakeZoom.Source = rtb;

			Canvas.SetLeft(fakeZoom, zPos.X);
			Canvas.SetTop(fakeZoom, zPos.Y);

			//Sposta le tile dietro alla fakemap (zindex)
			for (int i = 0; i < 42; i++)
			{
				Panel.SetZIndex(iAr[i], 1);
			}

			int tx, ty;

			//Acquisisce l'indice dell'eventuale rettangolo selezionato
			int mgIndex = -1;
			if (actMapGrid != null)
			{
				mgIndex = actMapGrid.index;
			}

			//Recupera l'indice della tile sul quale è posizionato il mouse nel vettore delle tile
			int pos = relativePixel2TileIndex(p);

			//Calcola gli indici della tile corrente
			tx = iAr[pos].x;
			ty = iAr[pos].y;

			//Calcola il pixel relativo alla tile sul quale è posizionato il mouse
			double mouseX = p.X - Canvas.GetLeft(iAr[pos]);
			double mouseY = p.Y - Canvas.GetTop(iAr[pos]);

			if (e.Delta > 0)    //Zoom in
			{
				zoom++;
				tiles *= 2;
				//individua gli indici e il pixel della nuova tile corrente
				tx *= 2;
				ty *= 2;
				mouseX *= 2;
				if (mouseX > 255)
				{
					tx++;
					mouseX -= 256;
				}
				mouseY *= 2;
				if (mouseY > 255)
				{
					ty++;
					mouseY -= 256;
				}
			}
			else    //Zoom out
			{
				zoom--;
				tiles /= 2;

				//individua gli indici e il pixel della nuova tile corrente
				mouseX = Math.Floor(mouseX / 2);
				mouseY = Math.Floor(mouseY / 2);
				if ((tx % 2) != 0) mouseX += 128;
				if ((ty % 2) != 0) mouseY += 128;

				tx = (int)Math.Floor(tx / 2.0);
				ty = (int)Math.Floor(ty / 2.0);

			}

			//sviluppo
			lockB.Content = zoom.ToString();
			///sviluppo

			p.X -= mouseX;
			p.Y -= mouseY;
			//Calcola indici e posizione della prima tile della nuova matrice
			while (p.X >= -512)
			{
				p.X -= 256;
				tx--;
				if (tx == -1)
				{
					tx = tiles - 1;
				}
			}
			while (p.Y >= -512)
			{
				p.Y -= 256;
				ty--;
				if (ty == -1)
				{
					ty = tiles - 1;
				}
			}
			//Rimuove i riquadri eventualmente presenti sulla mapp
			for (int i = 0; i < 10; i++)
			{
				if (canvas.Children.Contains(mgAr[i]))
				{
					canvas.Children.Remove(mgAr[i]);
				}
			}

			redrawCanvas(tx, ty, p);
			//canvas.MouseWheel += CanvasMouseWheel;
		}

		private void redrawCanvas(int tx, int ty, Point p)
		{
			int col = 0;
			int txStart = tx;
			double pxStart = p.X;
			for (int i = 0; i < 42; i++)
			{
				//iAr[i].redraw(zoom, tx, ty, p, ref canvas);
				iAr[i].zoom = zoom;
				iAr[i].x = tx;
				iAr[i].y = ty;
				iAr[i].p = p;
				col++;
				if (col == 7)
				{
					col = 0;
					tx = txStart;
					ty++;
					if (ty == tiles) ty = 0;
					p.Y += 256;
					p.X = pxStart;
				}
				else
				{
					tx++;
					if (tx == tiles) tx = 0;
					p.X += 256;
				}
			}

			iAr[16].redraw(ref canvas);
			iAr[17].redraw(ref canvas);
			iAr[18].redraw(ref canvas);
			iAr[23].redraw(ref canvas);
			iAr[24].redraw(ref canvas);
			iAr[25].redraw(ref canvas);

			for (int i = 0; i < 16; i++)
			{
				iAr[i].redraw(ref canvas);
				iAr[i + 26].redraw(ref canvas);
			}
			for (int i = 19; i < 23; i++)
			{
				iAr[i].redraw(ref canvas);
			}

		}

		private void fillCanvas(Point p)
		{
			//bAr[0] = new BitmapImage(new Uri("https://c.tile.openstreetmap.org/0/0/0.png"));
			tiles = (int)Math.Pow(2, zoom);
			int[] coord = coordinates2TileXY(p);
			int xn = coord[0];
			int yn = coord[1];
			int top = -512;
			int left = -512;
			if (canvas.Children.Contains(iAr[0]))
			{
				for (int i = 0; i < 42; i++)
				{
					canvas.Children.Remove(iAr[i]);
				}
			}

			//Calcola gli indici della tile in alto a sx

			for (int i = 0; i < 2; i++)
			{
				xn--;
				if (xn == -1) xn = tiles - 1;
				yn--;
				if (yn == -1) yn = tiles - 1;
			}
			xn--;
			if (xn == -1) xn = tiles - 1;
			xMin = xn;

			int col = 0;
			for (int i = 0; i < 42; i++)
			{
				iAr[i] = new TileImage(i, zoom, xn, yn, this);
				iAr[i].tiles = tiles;
				Canvas.SetLeft(iAr[i], left);
				Canvas.SetTop(iAr[i], top);

				left += 256;
				if (left == 1280)
				{
					left = -512;
					top += 256;
				}
				canvas.Children.Add(iAr[i]);

				xn++;
				if (xn == tiles) xn = 0;
				col++;
				if (col == 7)
				{
					col = 0;
					yn++;
					xn = xMin;
					if (yn == tiles) yn = 0;
				}
			}
		}

		private void spClick(object sender, MouseButtonEventArgs e)
		{
			stackPanelSelected(sender);
		}

		private void stackPanelSelected(object sender)
		{
			bool center = false;
			StackPanel sp;
			if (sender.GetType() == typeof(Tuple<StackPanel, int>))
			{
				sp = ((Tuple<StackPanel, int>)sender).Item1;
			}
			else
			{
				sp = (StackPanel)sender;
				center = true;
			}

			int index = Array.IndexOf(spAr, sp);

			if ((bool)cbAr[index].IsChecked)
			{
				for (int i = 0; i < 10; i++)
				{
					if (i != index)
					{
						spAr[i].Background = new SolidColorBrush(Color.FromArgb(0xff, 0x60, 0x60, 0x60));
					}
					else
					{
						spAr[i].Background = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0xaa, 0xde));
						actMapGrid = mgAr[i];

						if (center)
						{
							rendering = 1;
							removeMG();

							Point pos = new Point();

							pos.X = ((actMapGrid.B.X - actMapGrid.A.X) / 2) + actMapGrid.A.X;
							pos.Y = ((actMapGrid.B.Y - actMapGrid.A.Y) / 2) + actMapGrid.A.Y;

							for (int J = 19; J > 2; J--)
							{
								zoom = J;
								Point p1 = coordinates2AbsolutePixel(actMapGrid.B);
								Point p2 = coordinates2AbsolutePixel(actMapGrid.A);
								if ((p1.X - p2.X) < 768)
								{
									if ((p1.Y - p2.Y) < 580)
									{
										break;
									}
								}
							}
							fillCanvas(pos);

							pos = coordinates2TileXYDecimal(pos);
							pos.X = 128 - (int)((pos.X - Math.Truncate(pos.X)) * 256);
							pos.Y = 256 - (int)((pos.Y - Math.Truncate(pos.Y)) * 256) + 34;
							mousePosition = new Point(0, 0);
							moving = true;
							moveMap(pos);
							moving = false;
						}
						else
						{
							actMapGrid.catchEllipses();
						}
					}
				}
			}
		}

		private void removeMG()
		{
			for (int i = 0; i < 10; i++)
			{
				if (canvas.Children.Contains(mgAr[i]))
				{
					canvas.Children.Remove(mgAr[i]);
				}
				if (i < 8)
				{
					if (canvas.Children.Contains(ellAr[i]))
					{
						canvas.Children.Remove(ellAr[i]);
					}
				}
			}
		}

		public void rebuildMG()
		{
			for (int i = 0; i < 10; i++)
			{
				if (mgAr[i].placeOnMap())
				{
					cbAr[i].Checked -= cbChecked;
					cbAr[i].IsChecked = true;
					cbAr[i].Checked += cbChecked;
					spAr[i].Background = new SolidColorBrush(Color.FromArgb(0xff, 0x60, 0x60, 0x60));
				}
				else
				{
					cbAr[i].Checked -= cbChecked;
					cbAr[i].IsChecked = false;
					cbAr[i].Checked += cbChecked;
				}
			}
			if (actMapGrid != null)
			{
				spAr[actMapGrid.index].Background = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0xaa, 0xde));
				actMapGrid.catchEllipses();
			}
		}

		private void cbChecked(object sender, RoutedEventArgs e)
		{
			int index = 0;
			for (int i = 0; i < 10; i++)
			{
				spAr[i].Background = new SolidColorBrush(Color.FromArgb(0xff, 0x60, 0x60, 0x60));
				if (cbAr[i] == (CheckBox)sender)
				{
					index = i;
				}
			}
			spAr[index].Background = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0xaa, 0xde));
			try
			{
				for (int j = 0; j < 8; j++)
				{
					canvas.Children.Remove(ellAr[j]);
				}
			}
			catch { }
			mgAr[index].A = relativePixel2Coordinates(new Point(10, 10));
			mgAr[index].B = relativePixel2Coordinates(new Point(60, 60));
			if (mgAr[index].B.Y > mgAr[index].A.Y)
			{
				mgAr[index].A.Y = 85.05;
				double newY = (coordinates2RelativePixel(mgAr[index].A).Y) + 50;
				mgAr[index].B.Y = relativePixel2Coordinates(new Point(0, newY)).Y;
			}

			mgAr[index].placeOnMap();
			actMapGrid = mgAr[index];
			actMapGrid.catchEllipses();
		}

		private void cbUnchecked(object sender, RoutedEventArgs e)
		{
			for (int i = 0; i < 10; i++)
			{
				if (cbAr[i] == (CheckBox)sender)
				{
					spAr[i].Background = new SolidColorBrush(Color.FromArgb(0xff, 0x60, 0x60, 0x60));
					canvas.Children.Remove(mgAr[i]);
					mgAr[i].A = new Point(-214, -214);
					if (actMapGrid == mgAr[i])
					{
						actMapGrid.MouseLeftButtonDown -= sClick;
						actMapGrid = null;
						try
						{
							for (int j = 0; j < 8; j++)
							{
								canvas.Children.Remove(ellAr[j]);
							}
						}
						catch { }
					}
					break;
				}
			}
		}

		private void cbSchedChecked(object sender, RoutedEventArgs e)
		{
			foreach (TimePanel t in timePanelAr)
			{
				t.setAB();
			}
		}

		private void cbSchedUnchecked(object sender, RoutedEventArgs e)
		{
			foreach (TimePanel t in timePanelAr)
			{
				t.setA();
			}
		}

		private void enableChecked(object sender, RoutedEventArgs e)
		{
			bool atLeast = false;
			foreach (TimePanel t in timePanelAr)
			{
				if (t.isChecked)
				{
					atLeast = true;
					break;
				}
			}
			if (!atLeast)
			{
				MessageBox.Show("WARNING: when Geofencing is enabled, at least one time interval must be enabled.");
				timePanelAr[0].isChecked = true;
			}
		}

		private void toggleRecs(bool enabled)
		{
			for (int i = 0; i < 10; i++)
			{
				spAr[i].IsEnabled = !enabled;
			}
		}

		public static void saveCache(List<string> bitnameAr, List<SBitmap> sbitmapAr, string appDataPath)
		{
			foreach (string file in Directory.GetFiles(appDataPath, "*.png"))
			{
				if (!bitnameAr.Contains(System.IO.Path.GetFileName(file)))
				{
					try
					{
						File.Delete(file);
					}
					catch (Exception ex)
					{
						//sviluppo
						var b = ex;
						///sviluppo
					}
				}
			}

			for (int i = 0; i < sbitmapAr.Count; i++)
			{
				if (!File.Exists(appDataPath + bitnameAr[i]))
				{
					sbitmapAr[i].bitmap.Save(appDataPath + bitnameAr[i]);
				}
			}
		}
		//public void saveCache()
		//{
		//	if (!conn) return;

		//	foreach (string file in Directory.GetFiles(appDataPath, "*.png"))
		//	{
		//		if (!bitnameAr.Contains(System.IO.Path.GetFileName(file)))
		//		{
		//			try
		//			{
		//				File.Delete(file);
		//			}
		//			catch (Exception ex)
		//			{
		//				sviluppo
		//				var b = ex;
		//				/ sviluppo
		//			}
		//		}
		//	}

		//	for (int i = 0; i < sbitmapAr.Count; i++)
		//	{
		//		if (!File.Exists(appDataPath + bitnameAr[i]))
		//		{
		//			sbitmapAr[i].bitmap.Save(appDataPath + bitnameAr[i]);
		//		}
		//	}
		//}

		public override void copyValues()
		{

			if (mainEnableCB.IsChecked == false)
			{
				MessageBox.Show(String.Format("WARNING: Geofencing {0} not enabled!\r\nIf this is an intended behaviour, please ignore this warning.", index));
			}

			double co;
			MapGrid m;
			int c = 128;
			if (index == 2) c += 192;

			for (int i = 0; i < 10; i++)
			{
				m = mgAr[i];
				if (m.A.X < -200)
				{
					for (int j = 0; j < 16; j++)
					{
						conf[c + j] = 0;
					}
					conf[c] = 0x80;
				}
				else
				{
					co = m.A.X * 10000000;
					if (co < 0)
					{
						co += 0x100000000;
					}
					conf[c] = (byte)((uint)co >> 24);
					conf[c + 1] = (byte)((uint)co >> 16);
					conf[c + 2] = (byte)((uint)co >> 8);
					conf[c + 3] = (byte)co;

					co = m.A.Y * 10000000;
					if (co < 0)
					{
						co += 0x100000000;
					}
					conf[c + 4] = (byte)((uint)co >> 24);
					conf[c + 5] = (byte)((uint)co >> 16);
					conf[c + 6] = (byte)((uint)co >> 8);
					conf[c + 7] = (byte)co;

					co = m.B.X * 10000000;
					if (co < 0)
					{
						co += 0x100000000;
					}
					conf[c + 8] = (byte)((uint)co >> 24);
					conf[c + 9] = (byte)((uint)co >> 16);
					conf[c + 10] = (byte)((uint)co >> 8);
					conf[c + 11] = (byte)co;

					co = m.B.Y * 10000000;
					if (co < 0)
					{
						co += 0x100000000;
					}
					conf[c + 12] = (byte)((uint)co >> 24);
					conf[c + 13] = (byte)((uint)co >> 16);
					conf[c + 14] = (byte)((uint)co >> 8);
					conf[c + 15] = (byte)co;
				}
				c += 16;
			}
			conf[c + 3] = (byte)(sch[0] >> 24);
			conf[c + 2] = (byte)(sch[0] >> 16);
			conf[c + 1] = (byte)(sch[0] >> 8);
			conf[c] = (byte)sch[0];

			conf[c + 7] = (byte)(sch[1] >> 24);
			conf[c + 6] = (byte)(sch[1] >> 16);
			conf[c + 5] = (byte)(sch[1] >> 8);
			conf[c + 4] = (byte)sch[1];

			c += 8;
			for (int i = 0; i < 24; i++)
			{
				conf[c + i] = 0;
				if (timePanelAr[i].isChecked)
				{
					conf[c + i] = (byte)(timePanelAr[i].sel + 1);
				}
			}

			c = 512;
			if (index == 2) c = 513;
			conf[c] = 0;
			if (mainEnableCB.IsChecked == true)
			{
				conf[c] = 1;
			}
		}


		#region CONVERSIONI

		public Point relativePixel2AbsolutePixel(Point p)
		{
			int tile = relativePixel2TileIndex(p);
			Point pOut = new Point();
			pOut.X = iAr[tile].x * 256;
			pOut.X += p.X - Canvas.GetLeft(iAr[tile]);
			pOut.Y = iAr[tile].y * 256;
			double b = Canvas.GetTop(iAr[tile]);
			pOut.Y += p.Y - Canvas.GetTop(iAr[tile]);

			return pOut;
		}

		private int relativePixel2TileIndex(Point p)
		{
			int pos;
			int tx, ty;
			tx = 0;
			for (int i = 0; i < 7; i++)
			{
				double left = Canvas.GetLeft(iAr[i]);
				if ((p.X >= left) & (p.X < left + 256))
				{
					tx = i;
					break;
				}
			}
			ty = 0;
			for (int i = 0; i < 6; i++)
			{
				double top = Canvas.GetTop(iAr[i * 7]);
				if (p.Y >= top & p.Y < top + 256)
				{
					ty = i;
					break;
				}
			}
			pos = ty * 7 + tx;
			return pos;
		}

		public Point relativePixel2Coordinates(Point p)
		{
			double lonx, laty;
			int pos = relativePixel2TileIndex(p);
			double tx = iAr[pos].x;
			double ty = iAr[pos].y;
			int zoom = iAr[pos].zoom;
			tx += (p.X - Canvas.GetLeft(iAr[pos])) / 256;
			ty += (p.Y - Canvas.GetTop(iAr[pos])) / 256;
			lonx = tileX2Longitude(tx, zoom);
			laty = tileY2Latitude(ty, zoom);
			return new Point(lonx, laty);
			//return new double[] { lonx, laty };
		}

		public Point absolutePixel2Coordinates(Point p)
		{
			p.X /= 256;
			p.Y /= 256;
			return new Point(tileX2Longitude(p.X, zoom), tileY2Latitude(p.Y, zoom));
		}

		private int[] coordinates2TileXY(Point p)
		{
			p = coordinates2TileXYDecimal(p);
			return new int[] { (int)(Math.Floor(p.X)), (int)(Math.Floor(p.Y)) };
		}

		public Point coordinates2TileXYDecimal(Point pos)
		{
			double pixelX = (pos.X + 180.0) / 360.0 * (1 << zoom);
			double pixelY = (1 - Math.Log(Math.Tan(degree2Radians(pos.Y)) + 1 / Math.Cos(degree2Radians(pos.Y))) / Math.PI) / 2 * (1 << zoom);
			return new Point(pixelX, pixelY);
		}

		public Point coordinates2RelativePixel(Point p)
		{
			Point o = coordinates2AbsolutePixel(p);
			Point offset = relativePixel2AbsolutePixel(new Point(0, 0));

			o.X -= offset.X;
			o.Y -= offset.Y;

			if (o.X >= tiles * 256) o.X -= tiles * 256;
			if (o.X < 0) o.X += tiles * 256;

			if (o.Y >= tiles * 256) o.Y -= tiles * 256;
			if (o.Y < 0) o.Y += tiles * 256;

			return o;
		}

		public Point coordinates2AbsolutePixel(Point p)
		{
			Point o = coordinates2TileXYDecimal(p);
			o.X = o.X * 256;
			o.Y = o.Y * 256;

			return o;
		}

		public double tileX2Longitude(double x, int z)
		{
			return x / (double)(1 << z) * 360.0 - 180;
		}

		public double tileY2Latitude(double y, int z)
		{
			double n = Math.PI - 2.0 * Math.PI * y / (double)(1 << z);
			return 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
		}

		private double degree2Radians(double lat)
		{
			return (lat * (Math.PI / 180));
		}



		#endregion

	}
}
