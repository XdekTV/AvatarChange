using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO; // Dodano dla obsługi plików
using System.Security.Principal; // Dodano dla użycia WindowsIdentity
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32; // Dodano dla użycia Registry
using System.DirectoryServices.AccountManagement; // Dodano dla użycia PrincipalContext

namespace AvatarChange
{
    public partial class Form1 : Form
    {
        private PictureBox selectedPictureBox; // Przechowuje aktualnie zaznaczony PictureBox

        public Form1()
        {
            InitializeComponent();

            // Dodanie zdarzenia obsługi kliknięcia dla przycisku Cancel
            button1.Click += new EventHandler(button1_Click);

            // Dodanie zdarzenia obsługi kliknięcia dla linkLabel1
            linkLabel1.Click += new EventHandler(linkLabel1_Click);

            // Ładowanie obrazów z domyślnego folderu
            LoadImages(); // Dodanie metody ładowania obrazów

            // Załaduj aktualny awatar użytkownika
            LoadCurrentAvatar();

            // Rejestracja zdarzenia ładowania formularza
            this.Load += new EventHandler(Form1_Load); // Dodanie zdarzenia ładowania formularza

            // Rejestracja zdarzenia zamykania formularza
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing); // Dodanie zdarzenia zamykania formularza
        }

        // Metoda ustawiająca nazwę użytkownika w label3 oraz typ konta w label4
        private async void Form1_Load(object sender, EventArgs e)
        {
            // Ustaw nazwę użytkownika w label3
            label3.Text = $"{Environment.UserName}";

            // Przenieś sprawdzanie grupy użytkownika i hasła do osobnych metod asynchronicznych
            await Task.Run(() => CheckUserGroup());
            await Task.Run(() => CheckUserPassword());
        }

        private void CheckUserGroup()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);

            // Sprawdzenie, do jakiej grupy należy użytkownik
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                label4.Invoke((MethodInvoker)delegate { label4.Text = "Administrator"; });
            }
            else if (principal.IsInRole(WindowsBuiltInRole.User))
            {
                label4.Invoke((MethodInvoker)delegate { label4.Text = "Standard User"; });
            }
            else
            {
                label4.Invoke((MethodInvoker)delegate { label4.Text = "Other Group"; });
            }
        }

        // Metoda sprawdzająca, czy konto użytkownika ma hasło
        private void CheckUserPassword()
        {
            using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
            {
                // Sprawdzenie bieżącego użytkownika
                UserPrincipal user = UserPrincipal.FindByIdentity(context, Environment.UserName);
                if (user != null)
                {
                    // Sprawdzenie, czy konto ma hasło
                    bool hasPassword = user.LastPasswordSet.HasValue;

                    // Wywołanie na UI wątek
                    label5.Invoke((MethodInvoker)delegate {
                        if (hasPassword)
                        {
                            label5.Text = "Password protected"; // Ustawienie tekstu label5
                            label5.Visible = true; // Ustawienie label5 na widoczny
                        }
                        else
                        {
                            label5.Visible = false; // Ukryj label5, jeśli nie ma hasła
                        }
                    });
                }
                else
                {
                    label5.Invoke((MethodInvoker)delegate { label5.Visible = false; }); // Ukryj label5, jeśli nie znaleziono użytkownika
                }
            }
        }

        // Metoda zamykająca aplikację po kliknięciu przycisku Cancel
        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit(); // Zamknięcie aplikacji
        }

        // Obsługa zamknięcia aplikacji po kliknięciu "X"
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit(); // Zamknięcie aplikacji
        }

        // Metoda otwierająca okno dialogowe do wyboru obrazów po kliknięciu linkLabel1
        private void linkLabel1_Click(object sender, EventArgs e)
        {
            // Utwórz obiekt OpenFileDialog
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.bmp;*.jpg;*.jpeg;*.png",
                Title = "Select Picture",
                Multiselect = false // Umożliwienie wyboru jednego obrazu
            };

            // Wyświetl okno dialogowe i sprawdź, czy użytkownik wybrał plik
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                foreach (var file in openFileDialog.FileNames)
                {
                    AddImageToPanel(file);
                }
            }
        }

        // Metoda ładująca aktualny awatar użytkownika z rejestru
        private void LoadCurrentAvatar()
        {
            try
            {
                // Otwórz klucz rejestru
                using (RegistryKey usersKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\AccountPicture\Users"))
                {
                    if (usersKey != null)
                    {
                        // Pobierz SID użytkownika
                        string userSid = WindowsIdentity.GetCurrent().Owner.ToString();
                        Console.WriteLine("User SID: " + userSid); // Debugowanie SID

                        // Sprawdź, czy istnieje wpis dla aktualnego SID
                        using (RegistryKey userKey = usersKey.OpenSubKey(userSid))
                        {
                            if (userKey != null)
                            {
                                string userPicturePath = userKey.GetValue("Image192")?.ToString();
                                Console.WriteLine("User Picture Path: " + userPicturePath); // Debugowanie ścieżki do obrazu

                                // Sprawdź, czy plik istnieje
                                if (!string.IsNullOrEmpty(userPicturePath) && File.Exists(userPicturePath))
                                {
                                    pictureBox1.Image = Image.FromFile(userPicturePath);
                                    // Ustaw rozmiar pictureBox1 na 56x56
                                    pictureBox1.Width = 56;
                                    pictureBox1.Height = 56;
                                    pictureBox1.SizeMode = PictureBoxSizeMode.Zoom; // Upewnij się, że obrazek zachowuje proporcje
                                }
                                else
                                {
                                    // Ustaw pusty obrazek lub wyświetl komunikat, jeśli nie znaleziono pliku
                                    pictureBox1.Image = null; // Ustawia obrazek na pusty
                                    MessageBox.Show("Nie znaleziono awatara użytkownika. Ścieżka: " + userPicturePath);
                                }
                            }
                            else
                            {
                                // Dodano komunikat debugujący, który wyświetla dostępne SID w rejestrze
                                string[] subKeyNames = usersKey.GetSubKeyNames();
                                MessageBox.Show("Nie znaleziono wpisu rejestru dla SID: " + userSid +
                                                "\nDostępne SID w rejestrze: " + string.Join(", ", subKeyNames));
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Nie znaleziono klucza rejestru dla obrazów konta użytkownika.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Wystąpił błąd podczas ładowania awatara: " + ex.Message);
                pictureBox1.Image = null; // Ustawia obrazek na pusty
            }
        }

        // Metoda ładująca obrazy z domyślnego folderu
        private void LoadImages()
        {
            // Ścieżka do folderu z obrazami
            string folderPath = @"C:\ProgramData\Microsoft\User Account Pictures";

            // Sprawdź, czy folder istnieje
            if (Directory.Exists(folderPath))
            {
                // Pobierz pliki graficzne BMP, PNG, JPG z folderu
                string[] imageFiles = Directory.GetFiles(folderPath, "*.*")
                    .Where(file => file.ToLower().EndsWith(".bmp") ||
                                   file.ToLower().EndsWith(".png") ||
                                   file.ToLower().EndsWith(".jpg") ||
                                   file.ToLower().EndsWith(".jpeg"))
                    .ToArray();

                // Dodaj obrazy do flowLayoutPanel1
                foreach (string imageFile in imageFiles)
                {
                    AddImageToPanel(imageFile);
                }
            }
            else
            {
                MessageBox.Show("Folder nie istnieje!");
            }
        }

        // Metoda dodająca obraz do flowLayoutPanel1
        private void AddImageToPanel(string filePath)
        {
            // Utwórz nowy PictureBox
            PictureBox pictureBox = new PictureBox
            {
                Width = 48, // Ustaw rozmiar na 48x48
                Height = 48,
                Image = Image.FromFile(filePath),
                SizeMode = PictureBoxSizeMode.Zoom,
                Margin = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle // Opcjonalnie: Dodanie ramki do obrazków
            };

            // Dodanie obsługi zdarzenia kliknięcia
            pictureBox.Click += PictureBox_Click;
            pictureBox.Paint += PictureBox_Paint; // Dodajemy obsługę zdarzenia Paint

            // Dodaj PictureBox do flowLayoutPanel1
            flowLayoutPanel1.Controls.Add(pictureBox);
        }

        // Metoda obsługująca kliknięcia na PictureBox
        private void PictureBox_Click(object sender, EventArgs e)
        {
            // Przywróć oryginalne tło dla poprzednio zaznaczonego PictureBox
            if (selectedPictureBox != null)
            {
                selectedPictureBox.Invalidate(); // Wymuś ponowne narysowanie
            }

            // Zaznacz nowy PictureBox
            selectedPictureBox = sender as PictureBox; // Przypisz wybrany PictureBox do zmiennej
            selectedPictureBox.Invalidate(); // Wymuś ponowne narysowanie

            // Teraz można dodać dodatkowe operacje na zaznaczonym PictureBox, jeśli jest to wymagane
        }

        // Metoda rysująca zaznaczenie w PictureBox
        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            var pictureBox = sender as PictureBox;

            // Rysowanie prostokąta zaznaczenia, jeśli ten PictureBox jest zaznaczony
            if (pictureBox == selectedPictureBox)
            {
                using (var pen = new Pen(Color.Blue, 2)) // Ustawienie koloru i grubości ramki
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pictureBox.Width - 2, pictureBox.Height - 2);
                }
            }
        }
    }
}
