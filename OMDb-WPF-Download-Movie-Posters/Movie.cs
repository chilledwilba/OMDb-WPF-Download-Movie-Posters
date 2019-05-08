using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace OMDb_WPF_Download_Movie_Posters
{
    class Movie : ObservableObject
    {
        // File Information
        public string FileDir { get; set; }
        public string FileName { get; set; }
        public string FileMovieTitle { get; set; }
        public string FileYear { get; set; }
        public string FileQuality { get; set; }

        // OMDb Results
        public OMDbResults omdbResults { get; set; }

        // Did the poster Download Successfully
        public bool PosterDownload { get; set; } = false;

        #region Constructor
        public Movie(string fileName)
        {
            FileDir = fileName;

            fileName = Path.GetFileName(fileName);

            // Set Original File Name
            FileName = fileName;

            fileName = RemoveElements(fileName, new char[] { '.', '[', ']', '{', '}', '(', ')' });  // Remove Elements

            // Find Year
            FileYear = FindYear(fileName);

            // Find Movie Title Name
            FileMovieTitle = FindMovieTitle(fileName);
        }

        string RemoveElements(string name, char[] chars)
        {
            foreach (char chr in chars)
            {
                name = name.Replace(chr, ' ');
            }

            return name;
        }

        string FindYear(string name)
        {
            char[] chrs = name.ToCharArray();

            // Only need to check upto the 4th to last character, the year needs to be at least 4 characters long
            for (int i = 0; i < chrs.Length - 3; i++)
            {
                bool success = CheckNextFourNumbersAreDigits(chrs, i);

                if (success)
                {
                    char[] yearChrs = { chrs[i], chrs[i + 1], chrs[i + 2], chrs[i + 3] };
                    string yearString = new string(yearChrs);

                    if (int.Parse(yearString) > 1900)
                    {
                        return yearString;
                    }
                }
            }
            return null;
        }

        bool CheckNextFourNumbersAreDigits(char[] chr, int index)
        {
            for (int i = 0; i < 4; i++)
            {
                if (!char.IsDigit(chr[index + i]))
                {
                    return false;
                }
            }
            return true;
        }

        string FindMovieTitle(string name)
        {
            // Split File Name at Year, -
            string[] tmp = name.Split(new string[] { FileYear, "-" }, StringSplitOptions.None);
            string str;

            for (int i = 0; i < tmp.Length; i++)
            {
                str = tmp[i].Trim();

                if (str.Length > 0)
                {
                    return str;
                }
            }
            return name;
        }
        #endregion

        public void PropertyChange()
        {
            OnPropertyChanged(null);
        }

        public override string ToString()
        {
            return (FileName + " , " + FileMovieTitle + " , " + FileYear + " , " + FileQuality);
        }
    }
}
