using System.Windows.Forms;
using static Emgu.Util.Platform;

namespace Forms
{
    public partial class Form1 : Form
    {
        private readonly Dictionary<Button, MouseEventHandler> MoveEvent;
        private readonly Dictionary<Button, MouseEventHandler> UpEvent;
        private int ActiveCorner;
        private List<Point[]> CornersList;
        public static int InternalResulution;
        public static int OutputResulution;
        private readonly List<Image> imageQueue;
        private readonly List<Image> images;
        private static readonly int[] ResulutionIndex = new int[]{ 1920, 2560, 3840 };

        Config Config;
        
        public Form1()
        {
            InitializeComponent();

            Server server = new Server(42069, this);

            Thread network = new(new ThreadStart(new Action(() => { server.Loop(); })));

            Config = new(); 

            network.Start();

            comboBox1.SelectedIndex = Config.Data.OutputResulutionIndex;
            comboBox2.SelectedIndex = Config.Data.InternalResulutionIndex;

            MoveEvent = new Dictionary<Button, MouseEventHandler>();
            UpEvent = new Dictionary<Button, MouseEventHandler>();

            imageQueue = new List<Image>();
            images = new List<Image>();

            Resize += new EventHandler(ResizeMarkers);

            FormClosed += new FormClosedEventHandler((o, t) => { server.Close();});
        }
        private void GetCorners()
        {
            var corners = ImagePrep.getCorners(pictureBox1.Image, InternalResulution);
            if (corners.Count == 0) return;
            CornersList = corners;
            if (CornersList.Count > 1) ActiveCorner = 1;
            else ActiveCorner = 0;
            Invoke(new Action(() => { ResizeMarkers(null, null); }));

        }
        private void ResizeMarkers(object? sender, EventArgs? e)
        {
            if (CornersList == null || CornersList.Count == 0) return;
            put_marker(button6, CornersList[ActiveCorner][0]);
            put_marker(button7, CornersList[ActiveCorner][1]);
            put_marker(button8, CornersList[ActiveCorner][2]);
            put_marker(button9, CornersList[ActiveCorner][3]);
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            OutputResulution = ResulutionIndex[comboBox1.SelectedIndex];

            Config.Data.OutputResulutionIndex = comboBox1.SelectedIndex;
            Config.Save();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            InternalResulution = ResulutionIndex[comboBox1.SelectedIndex];

            Config.Data.InternalResulutionIndex = comboBox2.SelectedIndex;
            Config.Save();
        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void splitContainer2_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            button4_Click(null, null);
            button3_Click(null, null);
            button2_Click(null, null);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = ImagePrep.Contrast(pictureBox1.Image);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            float f = ImagePrep.GetFactor(pictureBox1.Image.Size, OutputResulution);

            Bitmap resized = new(pictureBox1.Image, new Size((int)(pictureBox1.Image.Width * f), (int)(pictureBox1.Image.Height * f)));

            Clipboard.SetImage(resized);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (imageQueue.Count > 0)
            {
                pictureBox1.Image = imageQueue[0];
                imageQueue.RemoveAt(0);
                GetCorners();
            }
        }

        public void SetLabel5(string msg)
        {
            label5.Text = msg;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = ImagePrep.Crop(pictureBox1.Image, CornersList[ActiveCorner]);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {
            GetCorners();

        }
        public static float[] getRatioAndOffset(Size orginalSize, Size currentSize)
        {
            float[] output = new float[3];

            Size Current = currentSize;
            Size Original = orginalSize;

            if (Current.Width / (float)Current.Height > Original.Width / (float)Original.Height)
            {
                output[0] = Current.Height / (float)Original.Height;
                output[1] = (int)((Current.Width - (Original.Width * output[0])) / 2f);
                output[2] = 0;
            }
            else
            {
                output[0] = Current.Width / (float)Original.Width;
                output[1] = 0;
                output[2] = (int)((Current.Height - (Original.Height * output[0])) / 2f);
            }

            return output;
        }

        private void put_marker(Button marker, Point point)
        {
            float[] rao = getRatioAndOffset(pictureBox1.Image.Size, pictureBox1.Size);

            Point pos = new Point((int)((point.X * rao[0]) + rao[1]), (int)((point.Y * rao[0]) + rao[2]));

            if (pos.X > 0 && pos.Y > 0 && pos.X < Size.Width - 30 && pos.Y < Size.Height - 50)
            {
                marker.Location = pos;
            }
            else
            {
                marker.Location = new Point(0, 0);
            }


        }

        private void markers_MouseDown(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            if (MoveEvent.ContainsKey(btn))
            {
                return;
            }
            FollowMouse(btn);
        }
        private new void Move(Button button)
        {
            Point pos = PointToClient(MousePosition);
            if (pos.X > 0 && pos.Y > 0 && pos.X < Size.Width - 30 && pos.Y < Size.Height- 50)
            {
                button.Location = pos;
            }
            else
            {
                button.Location = new Point(0, 0);
            }
                
        }

        private void FollowMouse(Button button)
        {
            groupBox1.Visible = false;
            MoveEvent.Add(button, new MouseEventHandler((s, e) => Move(button)));
            UpEvent.Add(button, new MouseEventHandler((s, e) => UnfollowMouse(button)));
            button.MouseMove += MoveEvent[button];
            button.MouseUp += UpEvent[button];
        }
        private void UnfollowMouse(Button button)
        {
            button.MouseMove -= MoveEvent[button];
            button.MouseUp -= UpEvent[button];

            _ = MoveEvent.Remove(button);
            _ = UpEvent.Remove(button);
            groupBox1.Visible = true;


            int num = int.Parse(button.Text) - 1;

            CornersList[ActiveCorner][num] = ScreenposToImagepos(button.Location);

        }
        private Point ScreenposToImagepos(Point screenpos)
        {
            float[] rao = getRatioAndOffset(pictureBox1.Image.Size, pictureBox1.Size);

            Point output = new((int)((screenpos.X - rao[1]) / rao[0]), (int)((screenpos.Y - rao[2]) / rao[0]));

            return output;
        }

        internal void SetImg(Image img)
        {
            if (pictureBox1.Image == null)
            {
                Invoke(new Action(() => { pictureBox1.Invalidate(); pictureBox1.Image = img; GetCorners(); }));
            }
            else
            {
                imageQueue.Add(img);
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            //prev
            if (CornersList == null) return;
            if (ActiveCorner > 0)
            {
                ActiveCorner--;
                ResizeMarkers(null, null);
            }

        }

        private void button10_Click(object sender, EventArgs e)
        {
            //next
            if (CornersList == null) return;
            if (ActiveCorner < CornersList.Count - 1)
            {
                ActiveCorner++;
                ResizeMarkers(null, null);
            }
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }

}