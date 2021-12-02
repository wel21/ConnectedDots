using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ConnectedDots
{
    public class ConnectedDots
    {
        private Random Rnd = new Random();
        private Dot[] Dots;
        private Point? CtrlMouseLocation;
        private System.Windows.Forms.Timer Tmr = new System.Windows.Forms.Timer { Enabled = false, Interval = 1 };
        private Control DisplayControl = null;

        #region Properties
        private Color dotColor = Color.FromArgb(250, 250, 250);
        private int dotCount = 100;
        private int dotDistance = 100;
        private float minSpeed = 0.1f;
        private float maxSpeed = 1.1f;

        public Color DotColor
        {
            get { return dotColor; }
            set { dotColor = value; }
        }
        public int DotCount
        {
            get { return dotCount; }
            set 
            { 
                dotCount = value;
                // resize Dots array on set
                Dots = new Dot[value + 1];
            }
        }
        public int DotDistance
        {
            get { return dotDistance; }
            set { dotDistance = value; }
        }
        public float MinSpeed
        {
            get { return minSpeed; }
            set { minSpeed = value; }
        }
        public float MaxSpeed
        {
            get { return maxSpeed; }
            set { maxSpeed = value; }
        }
        #endregion

        public ConnectedDots(Control ctrl)
        {
            DisplayControl = ctrl;

            // delegate control events
            DisplayControl.Paint += Ctrl_Paint;
            DisplayControl.MouseLeave += Ctrl_MouseLeave;
            DisplayControl.MouseMove += Ctrl_MouseMove;
            DisplayControl.Resize += Ctrl_Resize;

            Tmr.Enabled = false;
            Tmr.Tick += Tmr_Tick;
        }

        // start animation
        public void Start()
        {
            Tmr.Enabled = true;

            // Load dots on different thread for a delay effect
            Thread thrdDot = new Thread(LoadDots);
            thrdDot.Start();
        }

        private void LoadDots()
        {
            Dots = new Dot[DotCount + 1];
            for (int i = 0; i < Dots.Count(); i++)
            {
                // instantiate dots
                Dots[i] = new Dot(this, DisplayControl.ClientRectangle);
                // delay effect
                Thread.Sleep(300);
            }
        }

        #region Delegates
        private void Tmr_Tick(object sender, EventArgs e)
        {
            // reload control graphics
            DisplayControl.Invalidate();
        }

        private void Ctrl_Resize(object sender, EventArgs e)
        {
            DisplayControl = (Control)sender;
            foreach (Dot dot in Dots)
            {
                // set new rectangle to each dot
                if (dot != null)
                    dot.MainRect = DisplayControl.ClientRectangle;
            }
        }

        private void Ctrl_MouseMove(object sender, MouseEventArgs e)
        {
            // get current mouse location from the control
            CtrlMouseLocation = e.Location;
        }

        private void Ctrl_MouseLeave(object sender, EventArgs e)
        {
            CtrlMouseLocation = null;
        }

        private void Ctrl_Paint(object sender, PaintEventArgs e)
        {
            Graphics G = e.Graphics;
            G.SmoothingMode = SmoothingMode.AntiAlias;

            // process only if the timer is enabled and Dots is not null
            if (Tmr.Enabled && Dots != null)
            {
                for (int i = 0; i < Dots.Count(); i++)
                {
                    if (Dots[i] == null) continue;

                    if (CtrlMouseLocation != null)
                    {
                        // process mouse effect of Dots to avoid current mouse position range

                        int msx = CtrlMouseLocation.Value.X;
                        int msy = CtrlMouseLocation.Value.Y;
                        float osx = Dots[i].x;
                        float osy = Dots[i].y;

                        // calculate if the Dot is within range of current Moust Location
                        if (Math.Pow(msx - osx, 2) + Math.Pow(msy - osy, 2) < Math.Pow(DotDistance, 2))
                        {
                            // change the nex point of dot

                            // get the current angle of dot
                            int getAngle = (int)(((Math.Atan2(osx - msx, msy - osy) * (180 / Math.PI)) + 360.0) % 360);
                            // get the distance of dot from current mouse location
                            float getDist = DistanceBetween(new PointF(msx, msy), new PointF(osx, osy));
                            // set new point 
                            PointF newPoint = new PointF(GetX(osx, DotDistance - getDist, getAngle),
                                                         GetY(osy, DotDistance - getDist, getAngle));
                            Dots[i].x = newPoint.X;
                            Dots[i].y = newPoint.Y;
                        }
                    }

                    Dots[i].Update();
                    Dots[i].Show(G, DotColor);
                }
            }
        }
        #endregion

        internal class Dot
        {
            public int movementAngle;
            public float speed;
            public float size;
            public float x;
            public float y;
            public Rectangle MainRect;
            private ConnectedDots DotsParent;

            public Dot(ConnectedDots DotsParent, Rectangle MainRect)
            {
                this.DotsParent = DotsParent;
                this.MainRect = MainRect;
                ResetVars();
            }

            private void ResetVars()
            {
                movementAngle = DotsParent.Rnd.Next(0, 360);
                speed = (float)(DotsParent.Rnd.NextDouble() * (DotsParent.MaxSpeed - DotsParent.MinSpeed) + DotsParent.MinSpeed);
                size = DotsParent.Rnd.Next(1, 13);
                x = DotsParent.Rnd.Next(0, MainRect.Width);
                y = DotsParent.Rnd.Next(0, MainRect.Height);
            }

            // update dot position
            public void Update()
            {
                x = DotsParent.GetX(x, speed, movementAngle);
                y = DotsParent.GetY(y, speed, movementAngle);

                // reset all variables once the dot is outside the controls client rectangle
                if (x < -20 || y < -20 || x > MainRect.Width + 20 || y > MainRect.Height + 20)
                {
                    ResetVars();
                }
            }

            // draw dot
            public void Show(Graphics G, Color color)
            {
                PointF myPoint = new PointF(x, y);

                for (int i = 0;  i < DotsParent.Dots.Count(); i++)
                {
                    // avoid null dot
                    if (DotsParent.Dots[i] == null) continue;

                    PointF cpoint = new PointF(DotsParent.Dots[i].x, DotsParent.Dots[i].y);

                    if (cpoint.X != x && cpoint.Y != y)
                    {
                        float iDis = DotsParent.DistanceBetween(myPoint, cpoint);
                        if (iDis < DotsParent.DotDistance)
                        {
                            // set the alpha based on the distance. the farther the dot from the others, 
                            // the more transparent it will become until it disapear
                            int angle = (int)((iDis / DotsParent.DotDistance) * 50);
                            
                            using (Pen p = new Pen(Color.FromArgb(50 - angle, color), 0.5f))
                            {
                                G.DrawLine(p, myPoint, cpoint);
                            }
                        }
                    }
                }

                using (SolidBrush sb = new SolidBrush(Color.FromArgb(200, color)))
                {
                    G.FillEllipse(sb, new RectangleF(x - (size / 2), y - (size / 2), size, size));
                }
            }
        }

        #region Mathematics
        // used to calculate the distance between 2 points
        public float DistanceBetween(PointF p1, PointF p2)
        {
            double d1 = Math.Pow(Math.Abs(p2.X - p1.X), 2);
            double d2 = Math.Pow(Math.Abs(p2.Y - p1.Y), 2);
            return (float)Math.Sqrt(d1 + d2);
        }

        // used to calculate the x
        private float GetX(float FromX, float ToAdd, int Angle)
        {
            return FromX + ToAdd * (float)Math.Cos((Angle - 90 < 0 ? 360 + (Angle - 90) : Angle - 90) * Math.PI / 180);
        }

        // used to calculate the y
        private float GetY(float FromY, float ToAdd, int Angle)
        {
            return FromY + ToAdd * (float)Math.Sin((Angle - 90 < 0 ? 360 + (Angle - 90) : Angle - 90) * Math.PI / 180);
        }
        #endregion
    }
}
