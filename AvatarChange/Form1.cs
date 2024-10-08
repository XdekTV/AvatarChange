using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO; // Dodano dla obsługi plików
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        }

        // Metoda zamykająca aplikację po kliknięciu przycisku Cancel
        private void button1_Click(object sender, EventArgs e)
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
                Title = "Wybierz obraz",
                Multiselect = true // Umożliwienie wyboru wielu obrazów
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
                    e.Graphics.DrawRectangle(pen, 0, 0, pictureBox.Width - 1, pictureBox.Height - 1);
                }
            }
        }
    }
}
