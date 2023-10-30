using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using MinorShift.Emuera.Sub;
using System.Text.RegularExpressions;
using MinorShift._Library;

namespace MinorShift.Emuera.GameProc
{
    internal sealed partial class Process
    {
        public List<string> LazyloadDirectory = new List<string>();
        public List<string> LazyloadTarget = new List<string>();
        private string dirPath = Sys.ExeDir + "LLD.Config";
        private string filePath = Sys.ExeDir + "LLF.Config";
        public bool LazyloadDirecttoryCheck()
        {
            
            if (!File.Exists(dirPath))
                return false;
            EraStreamReader eReader = new EraStreamReader(false);
            if (!eReader.Open(dirPath))
                return false;
            try
            {
                string line = null;
                while ((line = eReader.ReadLine()) != null)
                {
                    if ((line.Length == 0) || (line[0] == ';'))
                        continue;
                    string line2 = Sys.ExeDir + line;
                    console.PrintSystemLine(line2+"을 리스트에 포함");
                    LazyloadDirectory.Add(line2);
                }
            }
            catch
            {

            }
            finally 
            {

            }
            return true;
        }
    }
}
