using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinorShift.Emuera.GameProc
{
    internal sealed partial class Process
    {
        public Dictionary<string, List<string>> LazyLoadingTable { get; private set; } = new Dictionary<string, List<string>>();
        public HashSet<string> LazyLoadingFiles { get; private set; } = new HashSet<string>();

        readonly static string LazyLoadingDataFilePath = Program.ExeDir + "lazyloading.dat";
        readonly static string LazyLoadingConfigFilePath = Program.ExeDir + "lazyloading.cfg";

        public bool TryLazyLoadErb(string functionName)
        {
            if (LazyLoadingTable.ContainsKey(functionName))
            {
                ErbLoader loader = new ErbLoader(console, exm, this);
                if (loader.loadErbs(LazyLoadingTable[functionName], labelDic))
                {
                    if (Program.AnalysisMode)
                    {
                        foreach (var str in LazyLoadingTable[functionName])
                            console.PrintSystemLine("구상 파일 로드: " + str);
                    }
                    LazyLoadingTable.Remove(functionName); // 로딩이 끝나면 해당 테이블 값은 필요가 없음.
                    return true;
                }
                else
                {
                    console.PrintSystemLine("관련된 구상 파일들을 읽어오는 데 실패했습니다: " + functionName);
                    return false;
                }
            }
            return false;
        }

        public IEnumerable<string> LoadLazyLoadingFolders()
        {
            if (!File.Exists(LazyLoadingConfigFilePath))
                return null; // 설정파일이 없으므로 일반 풀로딩을 해야 함.

            StreamReader reader = null;
            List<string> ret = new List<string>();
            try
            {
                reader = new StreamReader(LazyLoadingConfigFilePath, Encoding.UTF8);
                string line = null;
                while ((line = reader.ReadLine()) != null)
                    ret.Add(line.Trim());
            }
            catch (Exception e)
            {
                console.PrintSystemLine("지연로딩 설정 파일을 읽는 데 실패했습니다 : " + e.Message);
                return null;
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }

            return ret;
        }

        public bool LoadLazyLoadingTable()
        {
            if (!File.Exists(LazyLoadingDataFilePath))
                return false; // 설정파일이 없으므로 일반 풀로딩을 해야 함.

            StreamReader reader = null;
            try
            {
                reader = new StreamReader(LazyLoadingDataFilePath, Encoding.UTF8);
                string line = null;
                string[] tokens;
                while ((line = reader.ReadLine()) != null)
                {
                    tokens = line.Split(new char[] { '\t' }, 2);

                    if (!LazyLoadingTable.ContainsKey(tokens[0]))
                        LazyLoadingTable.Add(tokens[0], new List<string>());

                    string path = Program.ErbDir + tokens[1];
                    if (!LazyLoadingFiles.Add(path))
                        path = LazyLoadingFiles.First(x => x == path);
                    LazyLoadingTable[tokens[0]].Add(path); // 로딩할때 써야 하므로 상위경로를 넣어줘야 함.
                }
            }
            catch (Exception e)
            {
                console.PrintSystemLine("테이블을 읽는 데 실패했습니다 : " + e.Message);
                return false;
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }

            return true;
        }

        public bool SaveLazyLoadingList(List<FunctionLabelLine> labels, List<KeyValuePair<string, string>> erbFiles)
        {
            // 지연로딩 대상 폴더 목록을 읽고, 파일이 없거나 읽는 데 실패했다면 리턴.
            var paths = LoadLazyLoadingFolders();
            if (paths == null)
                return false;

            // 전체 파일 리스트에서 설정된 폴더 내에 있는 파일만 뽑아낸다.
            // erbFiles.key에는 ERB 폴더에서 시작하는 상대경로가 들어있으므로 이 값과 설정된 경로를 비교하면 된다.
            // https://stackoverflow.com/questions/4230313/linq-to-sql-join-and-contains-operators
            var ret = from pair in erbFiles
                      from path in paths
                      where pair.Key.StartsWith(path) == true //Substring(0, path.Length) == path 
                      select pair.Key;
            HashSet<string> files = new HashSet<string>(ret);

            // 메소드(#FUNCTION으로 정의되는) 함수와 이벤트 함수가 하나라도 있는 파일을 리스트에서 제외한다.
            foreach (FunctionLabelLine label in labels)
            {
                if (files.Contains(label.Position.Filename))
                {
                    if (label.IsMethod)
                    {
                        if (Program.AnalysisMode)
                            console.PrintSystemLine(label.Position.Filename + "의 " + label.LabelName + "함수에 #FUNCTION이 정의되어 있어 해당 파일을 제외합니다.");
                        files.Remove(label.Position.Filename);
                    }

                    if (label.IsEvent)
                    {
                        //						if (Program.AnalysisMode)
                        console.PrintSystemLine(label.Position.Filename + "에 이벤트 함수 " + label.LabelName + "가 있어 해당 파일을 제외합니다.");
                        files.Remove(label.Position.Filename);
                    }
                }
            }

            // 모든 함수에 대해 리스트에 있는 파일에 속해 있을 경우 리스트에 추가하고 저장한다.
            StreamWriter writer = null;

            try
            {
                writer = new StreamWriter(LazyLoadingDataFilePath, false, Encoding.UTF8);
                foreach (FunctionLabelLine label in labels)
                {
                    if (files.Contains(label.Position.Filename))
                        writer.WriteLine(label.LabelName + "\t" + label.Position.Filename);
                }
            }
            catch (Exception e)
            {
                console.PrintSystemLine("테이블 저장에 실패했습니다 : " + e.Message);
                return false;
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }

            return true;
        }

    }
}
