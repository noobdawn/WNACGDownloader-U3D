using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// 本子的信息
/// </summary>
public class NoteInfo
{
    public string Title;
    public string URL;
    public string TitleImage;
    public string Count;
    public Texture2D Tex;
    public string rarUrl;
}

public class WNACGDownloader : EditorWindow {
    [MenuItem("杂七杂八/WNACG下载")]
	public static void ShowWindow()
    {
        WNACGDownloader window = GetWindow<WNACGDownloader>();
        window.InitDownloader();
        window.Show();
    }

    private void InitDownloader()
    {
        if (!Directory.Exists(baseCachePath))
            Directory.CreateDirectory(baseCachePath);
        if (!Directory.Exists(baseDownPath))
            Directory.CreateDirectory(baseDownPath);
        MainWWW = null;
        noteDic = new Dictionary<string,NoteInfo>();
        waitForDownloadList = new List<NoteInfo>();
        cacheNote = null;
        downloadWWW = null;
        rarWWW = null;
    }

    private string baseWWWPath = "https://www.wnacg.com/";
    private string baseCachePath = "C:/wnacgCache";
    private string baseDownPath = "C:/wnacgDownload";
    private WWW MainWWW;
    private Dictionary<string, NoteInfo> noteDic;
    private List<NoteInfo> waitForDownloadList;
    private WWW downloadWWW;
    private WWW rarWWW;
    private NoteInfo cacheNote;
    private string baseUrl = "https://www.wnacg.com/albums-index-page-{0}.html";
    private int pageIdx = 1;
    void OnGUI()
    {
        ///处理下载页的部分
        if (downloadWWW != null && downloadWWW.isDone)
        {
            string zipUrl = GetZip(downloadWWW.text);
            Debug.Log("开始下载" + cacheNote.Title + " - " + zipUrl);
            rarWWW = new WWW(zipUrl);
            downloadWWW = null;
        }
        ///处理RAR的下载
        if (rarWWW != null && rarWWW.isDone)
        {
            FileStream fs = new FileStream(baseDownPath + "/" + cacheNote.Title + rarWWW.url.Substring(rarWWW.url.Length - 4), FileMode.CreateNew);
            Debug.Log("下载完成" + cacheNote.Title + " - " + cacheNote.Title + rarWWW.url.Substring(rarWWW.url.Length - 4));
            fs.Write(rarWWW.bytes, 0, rarWWW.bytes.Length);
            fs.Close();
            rarWWW = null;
            cacheNote = null;
        }
        ///若本子信息、下载页WWW、压缩包WWW均准备就绪，则开始下载
        if (cacheNote == null && downloadWWW == null && rarWWW == null && waitForDownloadList.Count > 0)
        {
            cacheNote = waitForDownloadList[0];
            waitForDownloadList.RemoveAt(0);
            Debug.Log("进入下载页：" + cacheNote.Title + " - " + cacheNote.URL);
            downloadWWW = new WWW(cacheNote.URL.Replace("photos", "download"));
        }

        if (MainWWW != null && MainWWW.isDone)
        {
            BuildNoteDic(MainWWW.text);
            MainWWW = null;
        } 
        ///显示部分
        ShowMainPageResult();
        ///控制部分
        GUILayout.BeginHorizontal();
        if (pageIdx > 1)
        {
            if (GUILayout.Button("上一页"))
            {
                pageIdx--;
            }
        }
        if (GUILayout.Button("下一页"))
        {
            pageIdx++;
        }
        pageIdx = int.Parse(GUILayout.TextField(pageIdx.ToString()));
        if (GUILayout.Button("跳转"))
        {
            Debug.Log("正在访问" + string.Format(baseUrl, pageIdx));
            MainWWW = new WWW(string.Format(baseUrl, pageIdx));
        }
        GUILayout.EndHorizontal();
    }

    void BuildNoteDic(string s)
    {
        //构建本子信息字典
        noteDic.Clear();
        if (s == null) return;
        s = s.Replace("\n", "");
        string[] lis = s.Split(new string[] { "<li class=\"li gallary_item\">" }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string li in lis)
        {
            string url = GetHref(li);
            if (string.IsNullOrEmpty(url)) continue;
            if (ExistNote(url)) continue;
            NoteInfo noteinfo = new NoteInfo();
            noteinfo.URL = url;
            noteinfo.Title = GetTitle(li);
            noteinfo.TitleImage = GetSrc(li);
            noteinfo.Count = GetCount(li);
            AddNote(noteinfo);
        }
    }

    void ShowMainPageResult()
    {
        //展示单个本子信息
        System.Action<NoteInfo, int, int> showSingleResult = (o, x, y) => {
            GUILayout.BeginArea(new Rect(x * 200, 50 + y * 100, 200, 100));
            GUILayout.BeginHorizontal();
            GUILayout.TextArea(o.Title, GUILayout.Width(200), GUILayout.Height(40));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("下载", GUILayout.Width(200)))
            {
                waitForDownloadList.Add(o);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(o.Count, GUILayout.Width(200));
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        };
        if (noteDic == null)
            return;
        int i = 0;
        foreach(NoteInfo info in noteDic.Values)
        {
            showSingleResult(info, i % 4, i / 4);
            i++;
        }
    }
    #region 提取信息
    string GetHref(string s)
    {
        Match m = Regex.Match(s, "\"title\"><a href=\".+.html");
        if (!m.Success) return string.Empty;
        return baseWWWPath + m.Value.Substring(18);
    }

    string GetSrc(string s)
    {
        Match m = Regex.Match(s, "src=\".+.jpg");
        if (!m.Success) return string.Empty;
        return baseWWWPath + m.Value.Substring(5);
    }

    string GetTitle(string s)
    {
        Match m = Regex.Match(s, "title=\".+<img alt=\"");
        if (!m.Success) return string.Empty;
        return m.Value.Substring(7, m.Value.Length - 19);
    }

    string GetCount(string s)
    {
        Match m = Regex.Match(s, "\\d*張照片");
        if (!m.Success) return string.Empty;
        return m.Value;
    }

    string GetZip(string s)
    {
        Match m = Regex.Match(s, "http://wnacg.*\" target=");
        if (!m.Success) return string.Empty;
        return m.Value.Substring(0, m.Value.Length - 9);
    }
    #endregion
    #region 字典的操作
    bool ExistNote(string url)
    {
        return noteDic.ContainsKey(url);
    }

    void AddNote(NoteInfo note)
    {
        if(!ExistNote(note.URL))
        {
            noteDic.Add(note.URL, note);
        }
    }

    void Remove(string url)
    {
        if (ExistNote(url))
            noteDic.Remove(url);
    }
#endregion
}
