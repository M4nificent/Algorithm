using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Intelligent_Scissors
{
    public partial class Form1 : Form
    {
        public static int ancX, ancY, distX, distY, X, Y, frequancy;
        public static RGBPixel[,] ImageMatrix,old;
        bool work;
        public static bool setclick = false;
        bool moving = false,lasseo=false,frflag=false;
        int coun = 0;
        public static bool valid = true;
        private void button3_Click(object sender, EventArgs e)
        {
            ancX = -1;
            ancY = -1;
            work = true;
            pictureBox2.Visible = false ;
        }

        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            if (moving == true)
            {
                pictureBox2.Refresh();
                //old = ImageMatrix;
                distX = e.X;
                distY = e.Y;
                ImageOperations.dij(ancX, ancY, old, distX, distY);
                ImageMatrix = ImageOperations.color_path(distX, distY, old, frequancy);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox2);                
            }
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (File.Exists("output.txt"))
            {
                File.Delete("output.txt");
            }
            int h = ImageOperations.GetHeight(ImageMatrix);
            int w = ImageOperations.GetWidth(ImageMatrix);
            using (StreamWriter outputfile = new StreamWriter("output.txt"))
            {
                outputfile.WriteLine("Constructed Graph: (Format: node_index|edges:(from, to, weight)... )");
                //outputfile.Write("\n");
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w; j++)
                    {

                       // outputfile.Write(" The  index node");
                        
                        outputfile.Write((i * w + j).ToString()+"|");
                       // outputfile.Write("\n");
                        outputfile.Write("edges:");
                        for (int t = 0; t < ImageOperations.graph[i*w+ j].Count(); t++)
                        {
                        outputfile.Write("(" + (i * w + j).ToString()+ "," + (ImageOperations.graph[i*w+ j][t].y+ ImageOperations.graph[i*w+ j][t].x*w)+ ","+ ImageOperations.graph[i*w+ j][t].energy+")");

                        }
                        outputfile.Write("\n");
                    }
                }
                
                double tt = Convert.ToDouble(ImageOperations.Takingtime.ElapsedMilliseconds);
                tt /= 1000;
                outputfile.WriteLine("Graph construction took: " + tt+" seconds");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (File.Exists("ShortestPath.txt"))
                File.Delete("ShortestPath.txt");
            int h = ImageOperations.GetHeight(ImageMatrix);
            int w = ImageOperations.GetWidth(ImageMatrix);
            ancX = Convert.ToInt32(textBox1.Text);
            ancY = Convert.ToInt32(textBox2.Text);
            distX = Convert.ToInt32(textBox3.Text);
            distY = Convert.ToInt32(textBox4.Text);
            ImageOperations.dij(ancX, ancY, ImageMatrix, distX, distY);
            ImageMatrix = ImageOperations.color_path(distX, distY, ImageMatrix, frequancy);
            ImageOperations.DisplayImage(ImageMatrix, pictureBox2);
            int j = distX;
            int i = distY;

            List<KeyValuePair<int, int>> l = new List<KeyValuePair<int, int>>();
            l.Add(new KeyValuePair<int, int>(i, j));
            while (true)
            {

            KeyValuePair<int, int> node = ImageOperations.par[i, j];
            i = node.Key;
            j = node.Value;
            if (i == -1) break;

                l.Add(node);
            }
            using (StreamWriter outputfile = new StreamWriter("ShortestPath.txt"))
            {
                outputfile.WriteLine("The Shortest path from Node " + (Convert.ToInt32(textBox1.Text) + Convert.ToInt32(textBox2.Text) * w).ToString() + " at (" + (textBox2.Text) + ", " + (textBox1.Text));
                outputfile.WriteLine(") to Node " + (Convert.ToInt32(textBox3.Text) + Convert.ToInt32(textBox4.Text) * w).ToString() + " at (" + (textBox4.Text) + ", " + (textBox3.Text)+ ")");
                outputfile.WriteLine("Format: (node_index, x, y)");
                for (i = l.Count()-1; i >= 0; i--)
                {
                    outputfile.WriteLine("{X=" + l[i].Value.ToString() + ",Y=" + l[i].Key.ToString() + "}," + l[i].Value.ToString() + "," + l[i].Key.ToString()+ ")");
                }
                //double tem = Convert.ToDouble(ImageOperations.Pathtime.ElapsedMilliseconds);
                // tem /= 1000;
                // outputfile.WriteLine(tem.ToString());
            }
            

        }

        private void freq_KeyUp(object sender, KeyEventArgs e)
        {
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
        }

        private void button7_Click(object sender, EventArgs e)
        {
            frflag = true;

        }

        private void pictureBox2_DoubleClick(object sender, EventArgs e)
        {
            moving = false;
        }

        private void freq_ValueChanged(object sender, EventArgs e)
        {
            frequancy = (int)freq.Value;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            lasseo = true;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            work = false;
            ImageOperations.dij(ancX, ancY, ImageMatrix, X, Y);
            ImageMatrix = ImageOperations.color_path(X, Y, ImageMatrix, frequancy);
            ImageOperations.DisplayImage(ImageMatrix, pictureBox2);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //ImageMatrix = old;
            //ImageOperations.DisplayImage(ImageMatrix, pictureBox2);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox2_MouseClick(object sender, MouseEventArgs e)
        {
            if (work == false) return;
            if (ancX == -1)
            {
                if (lasseo == true)
                    moving = true;
                ancX = e.X;
                ancY = e.Y;
                ImageMatrix = ImageOperations.put_anchor(ancY, ancX, old);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox2);
                X = ancX;
                Y = ancY;
                if (frflag == true)
                    frequancy = (int)freq.Value;
                //old = ImageMatrix;
            }
            else
            {

                // old = ImageMatrix;

                if (frflag == false)
                    setclick = true;
                if (frflag == true)
                {
                    moving = false;
                    frequancy = 0;
                    setclick = true;
                }


                 
                distX = e.X;
                distY = e.Y;
               // KeyValuePair<int, int> d = new KeyValuePair<int, int>(e.X, e.Y);
                ImageOperations.dij(ancX, ancY, old, distX, distY);
                ImageMatrix = ImageOperations.color_path(distX, distY, old, frequancy);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox2);
                ancX = distX;
                ancY = distY;
                ImageMatrix = ImageOperations.put_anchor(ancY, ancX, old);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox2);
            }
        }

        public Form1()
        {
            ancX = -1;
            ancY = -1;
            work = true;
            InitializeComponent();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            pictureBox2.Visible = true;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //Open the browsed image and display it
                string OpenedFilePath = openFileDialog1.FileName;
                ImageMatrix = ImageOperations.OpenImage(OpenedFilePath);
                ImageOperations.DisplayImage(ImageMatrix, pictureBox2);
            }
            txtWidth.Text = ImageOperations.GetWidth(ImageMatrix).ToString();
            txtHeight.Text = ImageOperations.GetHeight(ImageMatrix).ToString();
            ImageOperations.construct(ImageMatrix);
            old =(RGBPixel[,]) ImageMatrix.Clone();
            /*for(int i=0; i< ImageOperations.GetHeight(ImageMatrix); i++)
            {
                for (int j = 0; j < ImageOperations.GetWidth(ImageMatrix); j++)
                    old[i, j] = ImageMatrix[i, j];
            }*/
        }
    }
}
