using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Diagnostics;

namespace Chocorep2
{
    public class VersionManager
    {
        const string remoteurl = "http://ff14.room301.net/apps/chocorep2";
        const string versionfile = "version.xml";
        static string exefile = "chocorep2.exe";
        public static VersionInfoTable GetVersionInfo(string folder)
        {
            VersionInfoTable table = new VersionInfoTable();
            string[] files =Directory.GetFiles(folder,"*", SearchOption.AllDirectories);
            foreach (string _file in files)
            {
                string ext = Path.GetExtension(_file);
                string file = _file.Remove(0,Path.GetFullPath(folder).Length);
                if (ext == ".exe" || ext == ".dll")
                {
                    System.Diagnostics.FileVersionInfo vi =
                        System.Diagnostics.FileVersionInfo.GetVersionInfo(
                            file);
                    table.Rows.Add(file, vi.FileVersion);
                }
                else if (ext != ".config" && ext != ".pdb"&&ext != ".old" && ext != ".bat" && ext != ".zip" && file != "version.xml")
                {
                    table.Rows.Add(file,File.GetLastWriteTime(file).ToString("yyyyMMddHHmmss"));
                }
            }
            return table;
        }
        #region private method

        /// <summary>
        /// 再起動
        /// </summary>
        public static void Restart()
        {
            Process.Start(exefile, "/up " + Process.GetCurrentProcess().Id);
        }

        public static void VersionUp(string[] files)
        {
            foreach (string file in files)
            {
                string remotefile = GetRemoteFilePath(file);
                string oldfile =Path.Combine( Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".old");
                File.Delete(oldfile);
                if (File.Exists(file))
                {
                    File.Move(file, oldfile);
                }
                try
                {
                    Download(remotefile, file);
                    if (file.EndsWith(".exe"))
                    {
                        exefile = file;
                    }
                }
                catch(Exception _e)
                {//失敗したので元に戻す
                    File.Move(oldfile, file);
                    throw new Exception("バージョンアップに失敗しました。",_e);
                }
            }

        }

        public static void OverwriteLocalVersionFile(VersionInfoTable vt)
        {
            File.Delete(versionfile);
            vt.WriteXml(versionfile);
        }

        private static string GetRemoteFilePath(string filename)
        {
            return Path.Combine(remoteurl, filename);
        }

        public static string[] VersionCheck(VersionInfoTable remotev)
        {
            List<string> files = new List<string>();
            VersionInfoTable localv = new VersionInfoTable();
            if (File.Exists(versionfile))
            {
                try
                {
                    localv = VersionManager.ReadLocalVersion();
                }
                catch
                {
                }
            }
            foreach (DataRow remoterow in remotev.Rows)
            {
                DataRow localrow = localv.Rows.Find(remoterow["filename"]);
                if (localrow == null || localrow["version"].ToString() != remoterow["version"].ToString())
                {
                    files.Add(remoterow["filename"].ToString());
                }
            }
            return files.ToArray();
        }


        public static VersionInfoTable ReadRemoteVersion()
        {
            string url = Path.Combine(remoteurl, versionfile);
            VersionInfoTable vt = new VersionInfoTable();
            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                Stream st = wc.OpenRead(url);
                vt.ReadXml(st);
            }
            return vt;
        }

        public static VersionInfoTable ReadLocalVersion()
        {
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, versionfile);
            VersionInfoTable table = new VersionInfoTable();
            table.ReadXml(file);
            return table;
        }

        public static void CreateNewLocalVersionFile()
        {
            VersionInfoTable vt = VersionManager.GetVersionInfo(AppDomain.CurrentDomain.BaseDirectory);
            File.Delete(versionfile);
            vt.WriteXml(versionfile);
        }


        private static void Download(string url, string filename)
        {
            Microsoft.VisualBasic.Devices.Network network =
    new Microsoft.VisualBasic.Devices.Network();
            network.DownloadFile(
                url, filename,
                "", "",
                true, 6000, true,
                Microsoft.VisualBasic.FileIO.UICancelOption.DoNothing);
        }
        #endregion
    }

    public class VersionInfoTable : DataTable
    {
        public VersionInfoTable()
        {
            TableName = "versioninfo";
            DataColumn key = Columns.Add("filename", typeof(string));
            Columns.Add("version", typeof(string));
            PrimaryKey =new DataColumn[]{ key};
        }
    }


}
