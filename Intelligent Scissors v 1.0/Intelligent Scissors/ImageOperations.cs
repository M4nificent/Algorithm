using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.IO;
namespace Intelligent_Scissors
{
    /// <summary>
    /// Holds the pixel color in 3 byte values: red, green and blue
    /// </summary>
    public struct RGBPixel
    {
        public byte red, green, blue;
    }
    public struct RGBPixelD
    {
        public double red, green, blue;
    }

    /// <summary>
    /// Holds the edge energy between 
    ///     1. a pixel and its right one (X)
    ///     2. a pixel and its bottom one (Y)
    /// </summary>
    public struct Vector2D
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
    /// <summary>
    /// Holds the edge information between 2 nodes
    ///     1. x postion of the distenation node
    ///     2. y postion of the distenation node
    ///     3. energy = 1/pixel energy
    /// </summary>
    public struct edge
    {
        public int x, y;
        public double energy;
        public void setter(int _x, int _y, double E)
        {
            x = _x;
            y = _y;
            energy = 1.0 / E;
            if (E == 0.0) energy = 1E16;
        }
    }
    /// <summary>
    /// Library of static functions that deal with images
    /// </summary>
    class ImageOperations
    {
        public static Stopwatch Takingtime;
        public static Stopwatch Pathtime;
        public static List<edge>[] graph; // graph construction arraay
        public static KeyValuePair<int, int>[,] par; // parent array for backtraking path
        public static double[,] dist;
        /// <summary>
        /// Open an image and load it into 2D array of colors (size: Height x Width)
        /// </summary>
        /// <param name="ImagePath">Image file path</param>
        /// <returns>2D array of colors</returns>
        public static RGBPixel[,] OpenImage(string ImagePath)
        {
            Bitmap original_bm = new Bitmap(ImagePath);
            int Height = original_bm.Height;
            int Width = original_bm.Width;

            RGBPixel[,] Buffer = new RGBPixel[Height, Width];

            unsafe
            {
                BitmapData bmd = original_bm.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, original_bm.PixelFormat);
                int x, y;
                int nWidth = 0;
                bool Format32 = false;
                bool Format24 = false;
                bool Format8 = false;

                if (original_bm.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    Format24 = true;
                    nWidth = Width * 3;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format32bppArgb || original_bm.PixelFormat == PixelFormat.Format32bppRgb || original_bm.PixelFormat == PixelFormat.Format32bppPArgb)
                {
                    Format32 = true;
                    nWidth = Width * 4;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    Format8 = true;
                    nWidth = Width;
                }
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (y = 0; y < Height; y++)
                {
                    for (x = 0; x < Width; x++)
                    {
                        if (Format8)
                        {
                            Buffer[y, x].red = Buffer[y, x].green = Buffer[y, x].blue = p[0];
                            p++;
                        }
                        else
                        {
                            Buffer[y, x].red = p[0];
                            Buffer[y, x].green = p[1];
                            Buffer[y, x].blue = p[2];
                            if (Format24) p += 3;
                            else if (Format32) p += 4;
                        }
                    }
                    p += nOffset;
                }
                original_bm.UnlockBits(bmd);
            }

            return Buffer;
        }

        /// <summary>
        /// Get the height of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Height</returns>
        public static int GetHeight(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(0);
        }

        /// <summary>
        /// Get the width of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Width</returns>
        public static int GetWidth(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(1);
        }

        /// <summary>
        /// Calculate edge energy between
        ///     1. the given pixel and its right one (X)
        ///     2. the given pixel and its bottom one (Y)
        /// </summary>
        /// <param name="x">pixel x-coordinate</param>
        /// <param name="y">pixel y-coordinate</param>
        /// <param name="ImageMatrix">colored image matrix</param>
        /// <returns>edge energy with the right pixel (X) and with the bottom pixel (Y)</returns>
        public static Vector2D CalculatePixelEnergies(int x, int y, RGBPixel[,] ImageMatrix)
        {
            if (ImageMatrix == null) throw new Exception("image is not set!");

            Vector2D gradient = CalculateGradientAtPixel(x, y, ImageMatrix);

            double gradientMagnitude = Math.Sqrt(gradient.X * gradient.X + gradient.Y * gradient.Y);
            double edgeAngle = Math.Atan2(gradient.Y, gradient.X);
            double rotatedEdgeAngle = edgeAngle + Math.PI / 2.0;

            Vector2D energy = new Vector2D();
            energy.X = Math.Abs(gradientMagnitude * Math.Cos(rotatedEdgeAngle));
            energy.Y = Math.Abs(gradientMagnitude * Math.Sin(rotatedEdgeAngle));

            return energy;
        }

        /// <summary>
        /// Display the given image on the given PictureBox object
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <param name="PicBox">PictureBox object to display the image on it</param>
        public static void DisplayImage(RGBPixel[,] ImageMatrix, PictureBox PicBox)
        {
            // Create Image:
            //==============
            int Height = ImageMatrix.GetLength(0);
            int Width = ImageMatrix.GetLength(1);

            Bitmap ImageBMP = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            unsafe
            {
                BitmapData bmd = ImageBMP.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, ImageBMP.PixelFormat);
                int nWidth = 0;
                nWidth = Width * 3;
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (int i = 0; i < Height; i++)
                {
                    for (int j = 0; j < Width; j++)
                    {
                        p[0] = ImageMatrix[i, j].red;
                        p[1] = ImageMatrix[i, j].green;
                        p[2] = ImageMatrix[i, j].blue;
                        p += 3;
                    }

                    p += nOffset;
                }
                ImageBMP.UnlockBits(bmd);
            }
            PicBox.Image = ImageBMP;
        }
        /// <summary>
        /// Construct the graph
        /// </summary>
        /// <param name="grid">colored image matrix</param>
        public static void construct(RGBPixel[,] grid)
        {

            Takingtime = Stopwatch.StartNew();

            int h = GetHeight(grid); // o(1)
            int w = GetWidth(grid); // o(1)
            int n = h * w;
            graph = new List<edge>[n];

            // o(h*w)
            for (int i = 0; i < n; i++)
                graph[i] = new List<edge>();
            // o(h*w)
            int c = 0;
            for (int i = 0; i < n; i++)
            {

                int ii = i / w, jj = i % w;
                Vector2D E = CalculatePixelEnergies(jj, ii, grid);
                // o(1)
                if (ii != h - 1)
                {
                    edge temp = new edge();

                    temp.setter(ii + 1, jj, E.Y);
                    graph[c].Add(temp);
                    temp.setter(ii, jj, E.Y);
                    graph[(ii + 1) * w + jj].Add(temp);
                }
                if (jj != w - 1)
                {
                    edge temp = new edge();

                    temp.setter(ii, jj + 1, E.X);
                    graph[c].Add(temp);
                    temp.setter(ii, jj, E.X);
                    graph[c + 1].Add(temp);
                }
                c++;
            }
            Takingtime.Stop();

        }
        #region Private Functions
        /// <summary>
        /// Calculate Gradient vector between the given pixel and its right and bottom ones
        /// </summary>
        /// <param name="x">pixel x-coordinate</param>
        /// <param name="y">pixel y-coordinate</param>
        /// <param name="ImageMatrix">colored image matrix</param>
        /// <returns></returns>
        private static Vector2D CalculateGradientAtPixel(int x, int y, RGBPixel[,] ImageMatrix)
        {
            Vector2D gradient = new Vector2D();

            RGBPixel mainPixel = ImageMatrix[y, x];
            double pixelGrayVal = 0.21 * mainPixel.red + 0.72 * mainPixel.green + 0.07 * mainPixel.blue;

            if (y == GetHeight(ImageMatrix) - 1)
            {
                //boundary pixel.
                gradient.Y = 0;
            }
            else
            {
                RGBPixel downPixel = ImageMatrix[y + 1, x];
                double downPixelGrayVal = 0.21 * downPixel.red + 0.72 * downPixel.green + 0.07 * downPixel.blue;

                gradient.Y = pixelGrayVal - downPixelGrayVal;
            }

            if (x == GetWidth(ImageMatrix) - 1)
            {
                //boundary pixel.
                gradient.X = 0;

            }
            else
            {
                RGBPixel rightPixel = ImageMatrix[y, x + 1];
                double rightPixelGrayVal = 0.21 * rightPixel.red + 0.72 * rightPixel.green + 0.07 * rightPixel.blue;

                gradient.X = pixelGrayVal - rightPixelGrayVal;
            }



            return gradient;
        }

        #endregion
        /// <summary>
        /// Dijekstra implemntation to calculate the shortest path between source node and the rest of the nodes
        /// </summary>
        /// <param name="x">pixel x-coordinate</param>
        /// <param name="y">pixel y-coordinate</param>
        /// <param name="grid">colored image matrix</param>
        /// <returns>array hold the distance from the source to all other nodes</returns>
        public static void dij(int x, int y, RGBPixel[,] grid, int xd, int yd)
        {

            PriorityQueue pq = new PriorityQueue(false);
            KeyValuePair<double, KeyValuePair<int, int>> IN = new KeyValuePair<double, KeyValuePair<int, int>>(0, new KeyValuePair<int, int>(y, x));
            pq.push(IN);
            //zero based
            int H = GetHeight(grid);
            int W = GetWidth(grid);
            dist = new double[H, W];
            par = new KeyValuePair<int, int>[H, W];
            // O(H*W) 
            for (int i = 0; i < H; i++) for (int j = 0; j < W; j++) dist[i, j] = 1 / 0.0; // intilaize the array with infinity
            dist[y, x] = 0;
            KeyValuePair<int, int> parent = new KeyValuePair<int, int>(-1, -1);
            par[y, x] = parent;
            // O(E log (V)) 
            while (pq.empty() == false)
            {
                KeyValuePair<int, int> node = pq.Peek().Value;// O(1)
                double cost = pq.Peek().Key;
                if (node.Key == yd && node.Value == xd) return;
                pq.pop(); // O(log (v))
                if (cost != dist[node.Key, node.Value]) continue;
                for (int i = 0; i < graph[node.Key * W + node.Value].Count; i++)// as max edges between 2 nodes = 4 complextey for the loop O(log(v))
                {
                    edge E = graph[node.Key * W + node.Value][i];
                    if (dist[E.x, E.y] > dist[node.Key, node.Value] + E.energy)
                    {
                        parent = new KeyValuePair<int, int>(node.Key, node.Value);
                        par[E.x, E.y] = parent;
                        dist[E.x, E.y] = dist[node.Key, node.Value] + E.energy;
                        KeyValuePair<double, KeyValuePair<int, int>> temp = new KeyValuePair<double, KeyValuePair<int, int>>(dist[E.x, E.y], new KeyValuePair<int, int>(E.x, E.y));
                        pq.push(temp);// O(log(v))
                    }
                }
            }
            return;
        }
        /// <summary>
        /// Backtracking the path and color it
        /// </summary>
        /// <param name="xd">pixel x-coordinate for distination</param>
        /// <param name="yd">pixel y-coordinate for distenation</param>
        /// <param name="grid">colored image matrix</param>
        /// <returns>2D RGBPixel array hold the image after coloring</returns>
        public static RGBPixel[,] color_path(int xd, int yd, RGBPixel[,] grid, int fr)
        {
            Pathtime = Stopwatch.StartNew();
            List<KeyValuePair<int, int>> path = new List<KeyValuePair<int, int>>();
            int i = yd, j = xd;
            bool odd = false;
            // o(1) 
            int h = GetHeight(grid);
            int w = GetWidth(grid);
            RGBPixel W, B;
            RGBPixel[,] ret = (RGBPixel[,])grid.Clone();
            W.blue = 255;
            W.green = 255;
            W.red = 255;
            B.red = 0;
            B.green = 0;
            B.blue = 0;
            int c = 0;

            // o (n) (n length of the path) 
            while (true)
            {
                c++;
                if (odd == false)
                    ret[i, j] = B;
                else
                    ret[i, j] = W;
                if (c % 5 == 0)
                    odd ^= true;
                path.Add(new KeyValuePair<int, int>(i, j));
                KeyValuePair<int, int> node = par[i, j];
                if (node.Key == -1) break;
                i = node.Key;
                j = node.Value;
            }
            List<KeyValuePair<int, int>> l = new List<KeyValuePair<int, int>>();
            //O(N) (n length of the path)
            if (fr == 0)
            {
                if (Form1.setclick == true)
                {
                    put_anchor(path[path.Count - 1].Key, path[path.Count - 1].Value, grid);
                    Form1.ancY = path[path.Count - 1].Key;
                    Form1.ancX = path[path.Count - 1].Value;
                    for (int g = path.Count - 1; g >= 0; g--)
                    {
                        grid[path[g].Key, path[g].Value] = ret[path[g].Key, path[g].Value];
                    }
                    Form1.setclick = false;

                }
            }
            else
            {
                c = 0;
                //No.steps = 2N - > O(N) (n length of the path)
                for (int f = path.Count - 1, f1 = path.Count - 1; f >= 0; f--, c++)
                {

                    if (c % fr == 0)
                    {
                        //O(1)
                        put_anchor(path[f].Key, path[f].Value, grid);
                        Form1.ancY = path[f].Key;
                        Form1.ancX = path[f].Value;
                        //O(fr)
                        if (File.Exists("ShortestPath.txt"))
                            File.Delete("ShortestPath.txt");
                        using (StreamWriter outputfile = new StreamWriter("ShortestPath.txt"))
                        {
                            for (; f1 > f; f1--)
                            {
                                if (Form1.valid == true)
                                {
                                    outputfile.WriteLine("{X=" + path[f1].Value.ToString() + ",Y=" + path[f1].Key.ToString() + "},");
                                }
                                grid[path[f1].Key, path[f1].Value] = ret[path[f1].Key, path[f1].Value];
                            }
                            if (c == 50)
                            {
                                Form1.valid = false;
                                MessageBox.Show("end");
                            }
                        }

                    }
                }

            }
        Pathtime.Stop();
            return ret;
}
        /// <summary>
        ///
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="grid"></param>
        /// <returns></returns>
        
            //Theta(1)
        public static RGBPixel[,] put_anchor(int i, int j, RGBPixel[,] grid)
        {
            int h = GetHeight(grid);
            int w = GetWidth(grid);
            RGBPixel B;
            B.red = 0;
            B.green = 0;
            B.blue = 0;
            // 25 step - > O(1)
            for (int k = -2; k <= 2; k++)
            {
                for (int f = -2; f <= 2; f++)
                {
                    int ii = i + k, jj = j + f;
                    if (ii >= 0 && ii < h && jj >= 0 && jj < w)
                        grid[ii, jj] = B;
                }
            }
            return grid;
        }
    }
    /// <summary>
    /// PriorityQueue class using heap implemtation
    /// </summary>
    public class PriorityQueue
    {
        private List<KeyValuePair<double, KeyValuePair<int, int>>> list;// list holding the elemnts of the heap
        public int Count { get { return list.Count; } }
        public readonly bool IsDescending; // boolean to cntrol the heap sorting
        public bool empty() // return true if the queue is empty
        {
            return list.Count == 0;
        }
        public PriorityQueue() 
        {
            list = new List<KeyValuePair<double, KeyValuePair<int, int>>>();
        }

        public PriorityQueue(bool isdesc): this()
        {
            IsDescending = isdesc;
        }
        /// <summary>
        /// adding element to the queue
        /// </summary>
        /// <param name="x">value to add to queue</param>
        public void push(KeyValuePair<double, KeyValuePair<int, int>> x)
        {
            list.Add(x);// O(1)
            int i = Count - 1;
            //O(log(count)) maximum v number of vertices
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if ((IsDescending ? -1 : 1) * list[p].Key.CompareTo(x.Key) <= 0) break;

                list[i] = list[p];
                i = p;
            }

            if (Count > 0) list[i] = x;
        }
        /// <summary>
        /// pop the top element
        /// </summary>
        /// <returns>the top element</returns>
        public KeyValuePair<double, KeyValuePair<int, int>> pop()
        {
            KeyValuePair<double, KeyValuePair<int, int>> target = Peek();
            KeyValuePair<double, KeyValuePair<int, int>> root = list[Count - 1];
            list.RemoveAt(Count - 1);

            int i = 0;
            // O(log(count)) max = v
            while (i * 2 + 1 < Count)
            {
                int a = i * 2 + 1;
                int b = i * 2 + 2;
                int c = b < Count && (IsDescending ? -1 : 1) * list[b].Key.CompareTo(list[a].Key) < 0 ? b : a;

                if ((IsDescending ? -1 : 1) * list[c].Key.CompareTo(root.Key) >= 0) break;
                list[i] = list[c];
                i = c;
            }

            if (Count > 0) list[i] = root;
            return target;
        }
        /// <summary>
        /// return the top element
        /// </summary>
        /// <returns>the top element</returns>
        public KeyValuePair<double, KeyValuePair<int, int>> Peek()
        {
            if (Count == 0) throw new InvalidOperationException("Queue is empty.");
            return list[0];
        }
        /// <summary>
        /// clear the queue
        /// </summary>
        public void Clear()
        {
            list.Clear();
        }
    }


}
