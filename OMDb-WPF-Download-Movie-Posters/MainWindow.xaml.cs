using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;

using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;

namespace OMDb_WPF_Download_Movie_Posters
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<Movie> moviesObservableCollection = new ObservableCollection<Movie>();

        public MainWindow()
        {
            InitializeComponent();

            MoviesDG.ItemsSource = moviesObservableCollection;
        }

        #region Search Folder
        private void SelectFolder_Button_Click(object sender, RoutedEventArgs e)
        {
            if (GetDirectoryPath(out string directoryPath))
            {
                if (GetSubDirectoryPaths(directoryPath, out string[] folderFileNames))
                {
                    // Create Movie Objects
                    moviesObservableCollection.Clear();

                    foreach (string name in folderFileNames)
                    {
                        moviesObservableCollection.Add(new Movie(name));
                    }

                    // Set Label
                    SelectFolderLabel.Content = string.Format("Number of Movies: {0}", moviesObservableCollection.Count);
                }
            }
        }

        /// <summary>
        /// Open a File Dialog Box and return the selected path
        /// </summary>
        bool GetDirectoryPath(out string directoryPath)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                Multiselect = false,
                Title = "Select Movie Directory",
                FileName = "Movie's Folder",
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    directoryPath = Path.GetDirectoryName(dialog.FileName);
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("The process failed: {0}", e.ToString());
                }
            }

            directoryPath = null;
            return false;
        }

        /// <summary>
        /// Pass in a directory path and it will return a list of paths of the items within that directory
        /// </summary>
        bool GetSubDirectoryPaths(string path, out string[] subPaths)
        {
            try
            {
                subPaths = Directory.GetDirectories(path);
                subPaths = subPaths.Concat(Directory.GetFiles(path)).ToArray();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }
            subPaths = null;
            return false;
        }
        #endregion

        #region Search OMDb
        private async void SearchOMDB_Button_Click(object sender, RoutedEventArgs e)
        {
            string apiKey = APIKEYTextBox.Text;
            string url = string.Format("http://www.omdbapi.com/?apikey={0}&t={1}", apiKey, "1");
            bool validAPIKey = await Task.Run(() => CheckValidURL(url));

            if (validAPIKey)
            {
                foreach (Movie movie in moviesObservableCollection)
                {
                    await Task.Run(() => SearchOMDb(movie, apiKey));
                }

                int success = moviesObservableCollection.Count(i => (i.omdbResults.Response));

                // Set Label
                OMDbSearchLabel.Content = string.Format("Search | Success: {0} Fail: {1}", success, moviesObservableCollection.Count - success);

                // Message Box
                MessageBox.Show(string.Format("Successfully Found: {0} / {1}", success, moviesObservableCollection.Count));
            }
            else
            {
                MessageBox.Show(string.Format("Invalid API KEY: {0}", APIKEYTextBox.Text));
            }
        }

        void SearchOMDb(Movie movie, string apiKey)
        {
            string title = movie.FileMovieTitle.Replace(' ', '+');
            string year = movie.FileYear;
            string url = string.Format("http://www.omdbapi.com/?apikey={0}&t={1}&y={2}", apiKey, title, year);

            OMDbResults result = new OMDbResults
            {
                Response = false
            };

            if (CheckValidURL(url))
            {
                HttpClient httpClient = new HttpClient();
                var html = httpClient.GetStringAsync(url);

                try
                {
                    JsonConvert.PopulateObject(html.Result, result);
                }
                catch (Exception e)
                {
                    Console.WriteLine("The process failed: {0}", e.ToString());
                }

                httpClient.Dispose();
            }

            movie.omdbResults = result;
            movie.PropertyChange();
        }
        #endregion

        #region Download Posters
        private async void DownloadPosters_Button_Click(object sender, RoutedEventArgs e)
        {
            string url = string.Format("http://www.omdbapi.com/?apikey={0}&t={1}", APIKEYTextBox.Text, "1");
            bool validAPIKey = await Task.Run(() => CheckValidURL(url));

            if (validAPIKey)
            {
                if (GetDownloadDirectory(out string downloadDirectory))
                {
                    // Create Directory
                    Directory.CreateDirectory(downloadDirectory);

                    foreach (Movie movie in moviesObservableCollection)
                    {
                        if (movie.omdbResults.Response)
                        {
                            string fileName = GetFileName(movie, downloadDirectory);

                            // URLs
                            string urlAmazon = movie.omdbResults.Poster;
                            string urlOMDb = string.Format("http://img.omdbapi.com/?apikey={0}&i={1}", APIKEYTextBox.Text, movie.omdbResults.imdbID);

                            // Download Poster
                            bool success = await Task.Run(() => DownloadPoster(urlAmazon, fileName));
                            if (!success) success = await Task.Run(() => DownloadPoster(urlOMDb, fileName));

                            movie.PosterDownload = success;
                            movie.PropertyChange();
                        }
                    }

                    int successSearchOMDb = moviesObservableCollection.Count(i => (i.omdbResults.Response));
                    int successPosterDownload = moviesObservableCollection.Count(i => (i.PosterDownload));

                    // Set Label
                    DownloadPostersLabel.Content = string.Format("Download | Success: {0} Fail: {1}", successPosterDownload, successSearchOMDb - successPosterDownload);

                    // Message Box
                    MessageBox.Show(string.Format("Successfully Downloaded: {0} / {1}", successPosterDownload, successSearchOMDb));
                }
            }
            else
            {
                MessageBox.Show(string.Format("Invalid API KEY: {0}", APIKEYTextBox.Text));
            }
        }

        bool GetDownloadDirectory(out string downloadDirectory)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                Multiselect = false,
                Title = "Select Movie Poster Directory",
                FileName = "Poster Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    downloadDirectory = string.Format("{0}\\{1}", Path.GetDirectoryName(dialog.FileName), dialog.SafeFileName);
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("The process failed: {0}", e.ToString());
                }
            }

            downloadDirectory = null;
            return false;
        }

        string GetFileName(Movie movie, string downloadLocation)
        {
            string year = movie.omdbResults.Year;
            string title = MakeValidFileName(movie.omdbResults.Title);
            string fileExtension = ".jpg";
            return string.Format("{0}\\({1}) {2}{3}", downloadLocation, year, title, fileExtension);
        }

        private static string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "");
        }

        bool DownloadPoster(string url, string fileName)
        {
            bool success = false;

            if (CheckValidURL(url))
            {
                WebClient webClient = new WebClient();
                try
                {
                    webClient.DownloadFileAsync(new Uri(url), fileName);
                    success = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("The process failed: {0}", e.ToString());
                }

                webClient.Dispose();
            }

            return success;
        }
        #endregion

        private bool CheckValidURL(string url)
        {
            try
            {
                //Creating the HttpWebRequest
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                //Setting the Request method HEAD, you can also use GET too.
                request.Method = "HEAD";
                //Getting the Web Response.
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                //Returns TRUE if the Status code == 200
                response.Close();
                return (response.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                //Any exception will returns false.
                return false;
            }
        }
    }
}
