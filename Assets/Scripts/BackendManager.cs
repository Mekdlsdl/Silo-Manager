using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;

using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Unity.VisualScripting;
using System.Net;


public class BackendManager : MonoBehaviour
{
    private DatabaseReference reference;
    private readonly string uri = "";
    private FirebaseStorage storage;
    private StorageReference storageReference;
    private string allSiloData;

    private string userID;
    public string dateToday;

    public Text[] lengthTexts;
    public Text[] nameTexts;
    public Texture2D[] siloImages;
    public Text date;

    public GameObject loadingPanel;

    public bool isLoad = false;
    public bool updateDone = false;
    public bool setNameDone = false;

    public GameObject _UI;
    UIManager _UIG;

    public Text test;
    public string[] siloNames = new string[19];

    private void Awake()
    {
        _UIG = _UI.GetComponent<UIManager>();
        dateToday = DateTime.Today.ToString("yyyyMMdd");

        userID = SystemInfo.deviceUniqueIdentifier;


        FirebaseApp app;

        if (FirebaseApp.DefaultInstance == null)
        {
            AppOptions options = new AppOptions
            {
                DatabaseUrl = new Uri(uri),
                StorageBucket = ""
            };
 
            app = FirebaseApp.Create(options);
        }
        else
        {
            app = FirebaseApp.DefaultInstance;
        }

        storage = FirebaseStorage.DefaultInstance;
        storageReference = storage.GetReferenceFromUrl("");
    }

    void Start()
    {
        reference = FirebaseDatabase.DefaultInstance.RootReference.Child("SiloData");
    }


 
    // InsertData() - 데이터베이스에 데이터 추가

    public void InsertData(int siloNum, string siloName, string siloLength, bool floor)
    {
        string json = JsonUtility.ToJson(new Silo(userID, siloName, siloLength, floor));

        reference.Child(dateToday).Child(siloNum.ToString()).SetRawJsonValueAsync(json);
    }
    


    // ModifyName() - 이름을 수정한 경우 전체 이름 데이터베이스 수정

    public void ModifyName(int siloNum, string mSiloName)
    {
        DatabaseReference refer = reference.Child("Name");
        refer.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError(task.Exception.ToString());
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                refer.Child(siloNum.ToString()).SetValueAsync(mSiloName);
            }
        });
    }





    /*
     
        * 스토리지에 이미지 저장 기능
        
        UploadImage() - 스토리지에 이미지 추가
        ImageCheck() - 데이터베이스의 'Storage' 키에 파일 존재 여부 표시

    */

    public void UploadImage(int siloNum, byte[] siloImg)
    {
        string fileName = siloNum.ToString() + ".png";
        StorageReference uploadRef = storageReference.Child(dateToday).Child(fileName);

        uploadRef.PutBytesAsync(siloImg).ContinueWithOnMainThread((Task) =>
        {
            if (Task.IsFaulted || Task.IsCanceled)
            {
                Debug.Log(Task.Exception.ToString());
            }
            else
            {
                Debug.Log("이미지 업로드 성공");

                if (_UIG.addButtonText.text == "추  가")
                {
                    ImageCheck(siloNum);
                }
            }
        });
    }


    public void ImageCheck(int siloNum)
    {
        DatabaseReference refer = reference.Child("Storage");
        refer.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError(task.Exception.ToString());
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
 
                string updateData = siloNum.ToString();

                if (snapshot.HasChild(dateToday))
                {
                    IDictionary data = (IDictionary)snapshot.Value;
                    updateData = data[dateToday].ToString() + "," + siloNum.ToString();
                }

                refer.Child(dateToday).SetValueAsync(updateData);
            }
        });
    }




    /*
     
        * 과거 데이터 세팅 기능
        
        LoadPastDate() -  날짜 세팅, 과거 데이터 로딩 전 홈 화면 리셋

    */

    public void LoadPastDate(int dayAgo)
    {
        loadingPanel.gameObject.SetActive(true);

        for (int i = 0; i < lengthTexts.Length; i++)
        {
            siloImages[i] = null;
        }

        DateTime _d = DateTime.Today.AddDays(-dayAgo);

        date.text = _d.ToString("yyyy년 M월 d일 현재");
        dateToday = _d.ToString("yyyyMMdd");

        foreach (Text text in lengthTexts)
        {
            if (text != null)
            {
                text.text = "-0";
                text.color = Color.blue;
            };
        }

        _UIG.img.texture = null;

        StartCoroutine(GetInfo(dateToday));

        IsNotNullImage(dateToday);
    }





    /*
     
        * 데이터 세팅 기능
        
        GetInfo() - 서버로부터 해당 날짜의 모든 데이터 불러옴
        SetName() - 서버에 저장된 이름을 불러와 홈 화면에 적용
        UpdateAllData() - 각 사일로의 정보 세팅 (이름, 높이, 바닥 여부, 가득 여부)

    */

    public IEnumerator GetInfo(string inputDate)
    {
        SetName();

        yield return new WaitUntil(() => setNameDone);
        setNameDone = false;

        allSiloData = "";

        DatabaseReference refer = reference.Child(inputDate);
        refer.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError(task.Exception.ToString());
            }
            else if (task.IsCompleted)
            { 
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists && snapshot.ChildrenCount > 0)
                {

                    foreach (DataSnapshot data in snapshot.Children)
                    {
                        IDictionary _data = (IDictionary)data.Value;

                        string siloName = _data["siloName"] == null ? siloNames[int.Parse(data.Key)] : _data["siloName"].ToString();
                        allSiloData += $"{data.Key},{_data["siloLength"]},{_data["floor"]},{siloName}\t";
                    }

                }
                isLoad = true;
                StartCoroutine(UpdateAllData());
            }
        });
    }

    public void SetName()
    {
        DatabaseReference refer = reference.Child("Name");
        refer.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError(task.Exception.ToString());
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                foreach (DataSnapshot data in snapshot.Children)
                {
                    int key = int.Parse(data.Key);
                    siloNames[key] = data.Value.ToString().Replace("(", "\n(");
                    nameTexts[key].text = siloNames[key];
                }
                setNameDone = true;
            }
        });
    }

    IEnumerator UpdateAllData()
    {
        yield return new WaitUntil(() => isLoad);
        isLoad = false;

        foreach (string dt in allSiloData.Split('\t'))
        {
            string[] dtSplit = dt.Split(',');

            if (dtSplit.Length < 3)
            {
                continue;
            }

            int siloNum = int.Parse(dtSplit[0]);
            string siloLength = dtSplit[1];
            bool floor = bool.Parse(dtSplit[2]);
            string siloName;

            if (!dtSplit[3].Contains("\n("))
            {
                siloName = dtSplit[3].Replace("(", "\n(");
            }
            else
            {
                siloName = dtSplit[3];
            }

            lengthTexts[siloNum].color = floor ? Color.red : Color.blue;

            if (siloLength.Equals("FULL"))
            {
                lengthTexts[siloNum].color = Color.green;
                lengthTexts[siloNum].text = siloLength;
            }
            else
            {
                lengthTexts[siloNum].text = "-" + string.Format("{0:N0}", int.Parse(siloLength));
            }
            nameTexts[siloNum].text = siloName;
        }
        updateDone = true;
    }




    /*
     
        * 스토리지로부터 해당 날짜의 모든 이미지를 불러와 리스트에 저장하는 기능
        
        IsNotNullImage() - 존재하는 파일만 서버 호출할 수 있도록 체크
        GetImage() - 스토리지에서 해당 이미지 탐색
        LoadImage() - 스토리지에서 유니티로 이미지 가져와 저장
        LoadingOff() - 로딩 패널 off

    */

    public void IsNotNullImage(string date)
    {
        DatabaseReference refer = reference.Child("Storage");
        refer.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError(task.Exception.ToString());
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.HasChild(date))
                {
                    IDictionary data = (IDictionary)snapshot.Value;
                    string updateData = data[date].ToString();


                    foreach (string num in updateData.Split(','))
                    {
                        GetImage(int.Parse(num));
                    }

                    StartCoroutine(LoadingOff());
                }
                else
                {
                    StartCoroutine(LoadingOff());
                }
                
            }
        });
    }

    public void GetImage(int siloNum)
    {
        string fileName = siloNum.ToString() + ".png";
        StorageReference image = storageReference.Child(dateToday).Child(fileName);

        image.GetDownloadUrlAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsFaulted && !task.IsCanceled)
            {
                StartCoroutine(LoadImage(task.Result.ToString(), siloNum));
            }
            else
            {
                Debug.LogWarning($"Image not found for silo {siloNum}. Using default image or skipping.");
            }
        });
    }

    IEnumerator LoadImage(string url, int siloNum)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            siloImages[siloNum] = texture;
        }
        else
        {
            Debug.LogError($"Failed to load image from {url}: {request.error}");
        }
    }

    IEnumerator LoadingOff()
    {
        _UIG.images = siloImages;

        yield return new WaitForSeconds(1.0f);
        loadingPanel.gameObject.SetActive(false);
    }

    




    /*
      
        * 로그인 시 폐기할 데이터 자동 삭제 기능
    
        DeleteData(), DeleteStorage() - 지난 데이터, 이미지 자동 삭제
        CreateDateList() - 삭제하면 안 되는 데이터 제외

    */

    public void DeleteData()
    {
        List<string> dateList = CreateDateList("Storage", "Name");

        DatabaseReference refer = reference;
        refer.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError(task.Exception.ToString());
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                foreach (DataSnapshot data in snapshot.Children)
                {
                    if (!dateList.Contains(data.Key))
                    {
                        DatabaseReference databaseRef = refer.Child(data.Key);

                        databaseRef?.RemoveValueAsync().ContinueWith(t =>
                        {
                            if (t.IsCompleted)
                            {
                                Debug.Log($"Data for {data.Key} deleted successfully.");
                            }
                        });
                    }
                }
            }
        });
    }

    public void DeleteStorage()
    {
        List<string> dateList = CreateDateList("Reset");

        DatabaseReference refer = reference.Child("Storage");
        refer.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError(task.Exception.ToString());
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                
                foreach (DataSnapshot data in snapshot.Children)
                {
                    if (!dateList.Contains(data.Key))
                    {
                        foreach (string i in data.Value.ToString().Split(','))
                        {
                            string fileName = data.Key + "/" + i + ".png";
                            StorageReference storageRefer = storageReference.Child(fileName);

                            storageRefer.DeleteAsync().ContinueWith(task =>
                            {
                                if (task.IsCompleted)
                                {
                                    Debug.Log($"{date}/{i}.png 파일 삭제 성공");
                                }
                                else
                                {
                                    Debug.LogError($"Failed to delete file: {fileName}");
                                }
                            });
                        }

                        DatabaseReference DBStorageRef = reference.Child("Storage").Child(data.Key);

                        if (DBStorageRef != null)
                        {
                            DBStorageRef.RemoveValueAsync().ContinueWith(task => { });
                        }
                    }
                }
            }
        });
    }

    private List<string> CreateDateList(params string[] additionalKeys)
    {
        List<string> dateList = new List<string>(additionalKeys);

        for (int i = 0; i < 4; i++)
        {
            dateList.Add(DateTime.Today.AddDays(i - 3).ToString("yyyyMMdd"));
        }

        return dateList;
    }
}