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
        Dictionary<string, List<String>> TrainEventFiles = new Dictionary<string, List<string>>();
        Dictionary<string, List<String>> SkillFiles = new Dictionary<string, List<string>>();
        private string dirPath = Sys.ExeDir + "LLD.Config";
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
                    console.PrintSystemLine(line+"을 리스트에 포함");
                    LazyloadDirectory.Add(line);
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
        public bool LazyloadCheck(string path)
        {
            for  (int i = 0; i <= LazyloadDirectory.Count - 1;i++)
            {
                if (path.StartsWith(LazyloadDirectory[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public bool RegisterTrainEventFile(string relativePath, string fullPath)
        {
            // Config.GetFiles()는 파일명이 아니라 파일의 상대경로를 읽어오므로 여기서 파일명을 따로 분리해야 한다.
            string filename = Path.GetFileName(relativePath).ToUpper();

            if (filename.StartsWith("EVENT_"))
            {
                var token = filename.Split(new char[] { '_' }, 3);
                if (token.Length < 2) return false;

                if (token[1].StartsWith("K") || token[1].StartsWith("PUB") || token[1].StartsWith("SP"))
                {
                    if (!TrainEventFiles.ContainsKey(token[1]))
                        TrainEventFiles.Add(token[1], new List<string>());

                    TrainEventFiles[token[1]].Add(fullPath);

                    //console.PrintSystemLine("구상 파일이 Lazy Loading 테이블에 등록되었습니다 : " + relativePath);

                    return true;
                }
            }

            return false;
        }
        public bool LoadTrainEventFile(string functionName)
        {
            // 호출시점에 toupper()이 적용되어 있을 것임 아마도...
            if (functionName.StartsWith("EVENTTRAIN_")|| functionName.StartsWith("M_KOJO"))
            {
                var token = functionName.Split(new char[] { '_' }, 3);
                if (token.Length < 2) return false;

                if (token[1].StartsWith("K") || token[1].StartsWith("PUB") || token[1].StartsWith("SP") || token[1].StartsWith("M_KOJO"))
                {
                    if (TrainEventFiles.ContainsKey(token[1]))
                    {
                        ErbLoader loader = new ErbLoader(console, exm, this);
                        if (loader.loadErbs(TrainEventFiles[token[1]], labelDic))
                        {
                            TrainEventFiles.Remove(token[1]);
                            console.PrintSystemLine("관련된 구상 파일들을 로드했습니다: " + TrainEventFiles[token[1]]);
                            return true;
                        }
                        else
                        {
                            console.PrintSystemLine("관련된 구상 파일들을 읽어오는 데 실패했습니다: " + TrainEventFiles[token[1]]);
                            return false;
                        }
                    }
                }
            }

            return false;
        }
}
