// Author: Grant Nations
// Author: Sebastian Ramirez
// TankWars (main form) class for CS 3500 TankWars Client (PS8)

using System;
using System.Drawing;
using System.Windows.Forms;
using GameController;
using GameModel;
using TankWars;

namespace View
{
    /// <summary>
    /// Main view component of TankWars
    /// Handles user inputs and sends to controller for further use
    /// Controller initializes fields of the view when appropriate and tells view when to draw
    /// </summary>
    public partial class TankWars : Form
    {
        private Controller controller = new Controller();
        private DrawingPanel drawingPanel;
        private World world;

        private const int MenuSize = 60;
        private const int ViewSize = 900;

        private delegate void FrameEvent();

        public TankWars()
        {
            InitializeComponent();
            world = controller.GetWorld();
            controller.ErrorOccurred += ErrorOccurredMessage;
            controller.UpdateArrived += OnFrame;
            controller.WorldReady += InitializeDrawer;


            // Set up key and mouse handlers
            this.KeyDown += HandleKeyDown;
            this.KeyUp += HandleKeyUp;

        }
        /// <summary>
        /// Removes explosion id and frame counter from dictionary if tank disconnects.
        /// </summary>
        /// <param name="ID"></param>
        private void RemoveTankExplosionCount(int ID)
        {
            drawingPanel.GetExplosionCounter().Remove(ID);
        }
        /// <summary>
        /// Manages the frame counter for drawing a tank explosion animation: Sets the frame counter for a specific id to 0. Triggered when a tank dies
        /// </summary>
        /// <param name="ID"></param>
        private void SetExplosionCounter(int ID)
        {
            if (drawingPanel.GetExplosionCounter().TryGetValue(ID, out int counter))
            {
                counter = 0;
            }
            else
            {
                drawingPanel.GetExplosionCounter().Add(ID, 0);
            }
        }
        /// <summary>
        /// Initializes drawingPanel passing in the work and setting up event handlers
        /// </summary>
        private void InitializeDrawer()
        {
            // add the drawing panel
            drawingPanel = new DrawingPanel(world);
            drawingPanel.Location = new Point(0, MenuSize);
            drawingPanel.Size = new Size(ViewSize, ViewSize);
            MethodInvoker invoker = new MethodInvoker(() => this.Controls.Add(drawingPanel));
            this.Invoke(invoker);

            drawingPanel.MouseDown += HandleMouseDown;
            drawingPanel.MouseUp += HandleMouseUp;
            drawingPanel.MouseMove += HandleMouseMove;

            controller.RemoveTankExplosionCount += RemoveTankExplosionCount;
            controller.SetExplosionCounter += SetExplosionCounter;
            controller.SetBeamCounter += SetBeamCounter;
        }

        /// <summary>
        /// Manages the frame counter for drawing a beam animation: Sets the frame counter for a specific beam id to 0
        /// </summary>
        /// <param name="id"></param>
        private void SetBeamCounter(int id)
        {
            if (drawingPanel.GetBeamCounter().TryGetValue(id, out int counter))
            {
                counter = 0;
            }
            else
            {
                drawingPanel.GetBeamCounter().Add(id, 0);
            }

        }
        /// <summary>
        /// Handle mouse moving in the panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleMouseMove(object sender, EventArgs e)
        {

            controller.HandleMouseMove(drawingPanel.PointToClient(Cursor.Position), ViewSize);
        }

        /// <summary>
        /// Handle mouse up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleMouseUp(object sender, MouseEventArgs e)
        {
            controller.CancelMouseRequest(e);
        }

        /// <summary>
        /// Handle mouse down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleMouseDown(object sender, MouseEventArgs e)
        {
            controller.HandleMouseRequest(e);
        }


        /// <summary>
        /// Key up handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleKeyUp(object sender, KeyEventArgs e)
        {

            controller.CancelMoveRequest(e);
        }

        /// <summary>
        /// Key down handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HandleKeyDown(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == Keys.Escape)
                TankWars_FormClosing(sender, new FormClosingEventArgs(CloseReason.ApplicationExitCall, true));


            controller.HandleMoveRequest(e);

            //// Prevent other key handlers from running
            e.SuppressKeyPress = true;
            e.Handled = true;
        }

        /// <summary>
        /// Handler for the controller's UpdateArrived event
        /// </summary>
        private void OnFrame()
        {

            // Invalidate this form and all its children
            // This will cause the form to redraw as soon as it can

            MethodInvoker invoker = new MethodInvoker(() => this.Invalidate(true));

            this.Invoke(invoker);


        }
        /// <summary>
        /// If an error occurs during any point of executiong diplays appropiate message
        /// and allows the user to reconnect if desired
        /// </summary>
        /// <param name="message"></param>
        private void ErrorOccurredMessage(string message)
        {
            MethodInvoker invoker = new MethodInvoker(() =>
            {
                MessageBox.Show(message);
                connectButton.Enabled = true;
                IPTextBox.Enabled = true;
                playerNameTextBox.Enabled = true;
            });
            this.Invoke(invoker);
        }

        /// <summary>
        /// Event to handle connect button. Will not allow a blank or missing address name
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void connectButton_Click(object sender, EventArgs e)
        {
            if (IPTextBox.Text == "" || playerNameTextBox.Text == "")
            {
                MessageBox.Show("Address or player name cannot be blank");
                return;
            }
            connectButton.Enabled = false;
            IPTextBox.Enabled = false;
            playerNameTextBox.Enabled = false;
            controller.Connect(IPTextBox.Text, playerNameTextBox.Text);
        }


        /// <summary>
        /// Closes form with exit code 0 upon hitting the red X in the corner of the form for graceful exit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TankWars_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                Environment.Exit(0);
            }
            catch
            { }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("TankWars controls:\nUp: W \nDown: S \nLeft: A \nRight: D \nFire shot: left click \nFire Beam: Right Click \nExit: ESC");
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Additional Features: \nBeam animation changes colors and 3 smaller beam radiate out \nExplosion is animated flame \nOnly draws tanks, projectiles and powerups if they are within view distance of the player.");
        }
    }
}
