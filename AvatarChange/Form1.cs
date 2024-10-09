using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.DirectoryServices.AccountManagement;
using System.Net; // Added for NetworkCredential handling

namespace AvatarChange
{
    public partial class Form1 : Form
    {
        private PictureBoxWithPath selectedPictureBox; // Using a new class

        // Declaration of the LogonUser method
        [DllImport("AdvApi32.dll", SetLastError = true)]
        extern static bool LogonUser(string username, string password, string domain, UInt32 LogonType, UInt32 LogonProvider, ref IntPtr hToken);

        // Declarations for NetUserGetInfo and NetApiBufferFree
        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int NetUserGetInfo([MarshalAs(UnmanagedType.LPWStr)] string serverName, [MarshalAs(UnmanagedType.LPWStr)] string userName, int level, out IntPtr bufPtr);

        [DllImport("Netapi32.dll")]
        private static extern int NetApiBufferFree(IntPtr Buffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct USER_INFO_1
        {
            public string usri1_name;
            public string usri1_password;
            public uint usri1_password_age;
            public uint usri1_priv;
            public string usri1_home_dir;
            public string usri1_comment;
            public uint usri1_flags;
            public string usri1_script_path;
        }

        private const int NERR_Success = 0;
        private const uint UF_PASSWD_NOTREQD = 0x00000020;
        private const uint UF_ACCOUNTDISABLE = 0x00000002;

        // Constants for user privilege levels (Administrator or Standard User)
        private const uint USER_PRIV_GUEST = 0;
        private const uint USER_PRIV_USER = 1;
        private const uint USER_PRIV_ADMIN = 2;

        public Form1()
        {
            InitializeComponent();

            button1.Click += new EventHandler(button1_Click);
            linkLabel1.Click += new EventHandler(linkLabel1_Click);
            LoadImages(); // Loading images from folder
            LoadCurrentAvatar(); // Loading user avatar

            this.Load += new EventHandler(Form1_Load);
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            label3.Text = $"{Environment.UserName}";
            await Task.Run(() => CheckUserGroup());
            await Task.Run(() => CheckUserPassword());
        }

        // Updated CheckUserGroup method using NetUserGetInfo
        private void CheckUserGroup()
        {
            IntPtr bufPtr = IntPtr.Zero;

            try
            {
                // Retrieve information about the currently logged-in user
                int result = NetUserGetInfo(null, Environment.UserName, 1, out bufPtr);
                if (result == NERR_Success)
                {
                    // Convert the buffer to USER_INFO_1 structure
                    USER_INFO_1 userInfo = (USER_INFO_1)Marshal.PtrToStructure(bufPtr, typeof(USER_INFO_1));

                    // Check user privilege level
                    label4.Invoke((MethodInvoker)delegate {
                        switch (userInfo.usri1_priv)
                        {
                            case USER_PRIV_ADMIN:
                                label4.Text = "Administrator";
                                break;
                            case USER_PRIV_USER:
                                label4.Text = "Standard User";
                                break;
                            case USER_PRIV_GUEST:
                                label4.Text = "Guest";
                                break;
                            default:
                                label4.Text = "Unknown";
                                break;
                        }
                    });
                }
                else
                {
                    MessageBox.Show("Cannot retrieve user group information.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while checking user group: {ex.Message}");
            }
            finally
            {
                // Free the buffer to avoid memory leaks
                if (bufPtr != IntPtr.Zero)
                {
                    NetApiBufferFree(bufPtr);
                }
            }
        }

        // Updated CheckUserPassword method
        private void CheckUserPassword()
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Machine)) // or ContextType.Domain if you are in a domain
                {
                    var user = UserPrincipal.FindByIdentity(context, Environment.UserName);
                    if (user != null)
                    {
                        label5.Invoke((MethodInvoker)delegate {
                            // Check if password is required
                            bool passwordNotRequired = user.PasswordNotRequired;
                            bool passwordNeverExpires = user.PasswordNeverExpires;

                            // If password is not required, hide label5
                            if (passwordNotRequired)
                            {
                                label5.Visible = false;
                            }
                            else if (passwordNeverExpires || user.LastPasswordSet != null)
                            {
                                // If password is set (LastPasswordSet is not null), show label5
                                label5.Text = "Password protected";
                                label5.Visible = true;
                            }
                            else
                            {
                                // If password is not set
                                label5.Visible = false;
                            }
                        });
                    }
                    else
                    {
                        MessageBox.Show("Cannot find the user.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while checking user password: {ex.Message}");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void linkLabel1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png",
                Title = "Select Picture",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                foreach (var file in openFileDialog.FileNames)
                {
                    AddImageToPanel(file);
                }
            }
        }

        private void LoadCurrentAvatar()
        {
            try
            {
                string userSid = WindowsIdentity.GetCurrent().User.ToString();
                string avatarDirectory = Path.Combine(@"C:\Users\Public\AccountPictures", userSid);

                if (Directory.Exists(avatarDirectory))
                {
                    string[] extensions = new[] { ".jpg", ".png", ".bmp" };
                    string avatarPath = null;
                    var files = Directory.GetFiles(avatarDirectory);

                    foreach (var file in files)
                    {
                        if (file.EndsWith("-Image192.jpg", StringComparison.OrdinalIgnoreCase) ||
                            file.EndsWith("-Image192.png", StringComparison.OrdinalIgnoreCase) ||
                            file.EndsWith("-Image192.bmp", StringComparison.OrdinalIgnoreCase))
                        {
                            avatarPath = file;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(avatarPath))
                    {
                        pictureBox1.Image = Image.FromFile(avatarPath);
                        pictureBox1.Width = 56;
                        pictureBox1.Height = 56;
                        pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                    }
                    else
                    {
                        MessageBox.Show("User avatar not found.");
                        pictureBox1.Image = null;
                    }
                }
                else
                {
                    MessageBox.Show("Avatar directory not found: " + avatarDirectory);
                    pictureBox1.Image = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while loading avatar: " + ex.Message);
                pictureBox1.Image = null;
            }
        }

        private void LoadImages()
        {
            string folderPath = @"C:\ProgramData\Microsoft\User Account Pictures";

            if (Directory.Exists(folderPath))
            {
                string[] imageFiles = Directory.GetFiles(folderPath, "*.*")
                    .Where(file => file.ToLower().EndsWith(".bmp") ||
                                   file.ToLower().EndsWith(".png") ||
                                   file.ToLower().EndsWith(".jpg") ||
                                   file.ToLower().EndsWith(".jpeg"))
                    .ToArray();

                foreach (var filePath in imageFiles)
                {
                    AddImageToPanel(filePath);
                }
            }
            else
            {
                MessageBox.Show("Image folder does not exist: " + folderPath);
            }
        }

        private void AddImageToPanel(string filePath)
        {
            PictureBoxWithPath pictureBox = new PictureBoxWithPath
            {
                Width = 48,
                Height = 48,
                Image = Image.FromFile(filePath),
                SizeMode = PictureBoxSizeMode.Zoom,
                Margin = new Padding(0),
                BackColor = Color.Transparent,
                ImagePath = filePath // Setting the image path
            };

            pictureBox.Click += PictureBox_Click;

            Panel panel = new Panel
            {
                Width = 56,
                Height = 56,
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None
            };

            panel.Controls.Add(pictureBox);
            flowLayoutPanel1.Controls.Add(panel);
        }

        private void PictureBox_Click(object sender, EventArgs e)
        {
            PictureBoxWithPath clickedPictureBox = (PictureBoxWithPath)sender;

            if (selectedPictureBox != null && selectedPictureBox != clickedPictureBox)
            {
                RemoveBorder(selectedPictureBox);
            }

            selectedPictureBox = clickedPictureBox;
            DrawBorder(selectedPictureBox, Color.LightBlue);
            selectedPictureBox.Invalidate();
        }

        private void DrawBorder(PictureBoxWithPath pictureBox, Color color)
        {
            Bitmap bitmap = new Bitmap(pictureBox.Width, pictureBox.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(pictureBox.Image, 0, 0, pictureBox.Width, pictureBox.Height);
                using (Pen pen = new Pen(color, 2))
                {
                    g.DrawRectangle(pen, 1, 1, pictureBox.Width - 2, pictureBox.Height - 2);
                }
            }
            pictureBox.Image = bitmap;
        }

        private void RemoveBorder(PictureBoxWithPath pictureBox)
        {
            // Restore the original image
            if (File.Exists(pictureBox.ImagePath))
            {
                pictureBox.Image = Image.FromFile(pictureBox.ImagePath);
                pictureBox.Invalidate();
            }
        }
    }

    public class PictureBoxWithPath : PictureBox
    {
        public string ImagePath { get; set; } // Property to store the image path
    }
}
