using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using UnityEngine.Android;

using Firebase;
using Firebase.Database;
using System.Runtime.CompilerServices;

public class UIManager : MonoBehaviour
{
    public Text date;

    public Text lengthPH;
    public Text namePH;
    public GameObject infoPanel;

    public Text[] dates;

    Text siloLength = null;
    public Text lengthInput;
    string originalName;
    public Text nameInput;

    WebCamTexture camTexture;
    public RawImage camImg;
    public GameObject tapButton;

    public RawImage img;
    public Texture2D[] images;
    Texture2D tempImage;
    byte[] tempBytes;

    int siloNum;
    public Text addButtonText;
    public Toggle floorCheck;

    public GameObject _BM;
    private BackendManager _BMG;

    void Start()
    {
        _BMG = _BM.GetComponent<BackendManager>();
        date.text = DateTime.Today.ToString("yyyy년 M월 d일 현재");

        for (int i = 0; i < 3; i++)
        {
            dates[i].text = DateTime.Today.AddDays(i-3).ToString("M월 d일");
        }
    }

    /*

        * 상세 창을 켰을 때 보일 데이터 세팅 기능

        OriginalName() - 누른 사일로의 이름 받아오기
        SetData() - 상세 창 정보 세팅

    */
    public void OriginalName(Text original)
    {
        originalName = original.text;
    }

    public void SetData(Text originalText)
    {
        siloLength = originalText;
        lengthPH.text = originalText.text.Replace("-", "");
        namePH.text = originalName.Replace("\n(", "(");

        siloNum = int.Parse(siloLength.name);

        Texture2D siloImage = images[siloNum];

        if (siloImage)
        {
            img.texture = siloImage;
            addButtonText.text = "수  정";
        }

        if (originalText.color == Color.red)
        {
            floorCheck.isOn = true;
        }
        else
        {
            floorCheck.isOn = false;
        }
    }

    public void FullButtonClick()
    {
        lengthPH.text = "FULL";
        siloLength.text = "FULL";
        siloLength.color = Color.green;
    }

    public void SaveButtonClick()
    {
        StartCoroutine(SaveData());
    }

    public IEnumerator SaveData()
    {
        string saveLength;
        string saveName;

        if (!string.IsNullOrEmpty(lengthInput.text))
        {
            saveLength = lengthInput.text;
            siloLength.text = "-" + string.Format("{0:N0}", int.Parse(lengthInput.text));
        }
        else
        {
            saveLength = siloLength.text.Replace("-", "").Replace(",", "");
        }

        if (!string.IsNullOrEmpty(nameInput.text))
        { 
            saveName = nameInput.text;
        }
        else
        {
            saveName = originalName;
        }

        _BMG.InsertData(siloNum, saveName, saveLength, floorCheck.isOn);
        _BMG.ModifyName(siloNum, saveName);

        if (tempImage != null)
        {
            images[siloNum] = tempImage;
            _BMG.UploadImage(siloNum, tempBytes);
        }
        
        if (floorCheck.isOn)
        {
            siloLength.color = Color.red;
        }
        else
        {
            siloLength.color = Color.blue;
        }

        string dateToday = _BMG.dateToday;
        StartCoroutine(_BMG.GetInfo(dateToday));

        yield return new WaitUntil(() => _BMG.updateDone);
        _BMG.updateDone = false;

        ExitButtonClick();
    }

    public void ExitButtonClick()
    {
        img.texture = null;
        addButtonText.text = "추  가";
        floorCheck.isOn = false;
        tempImage = null;
        tempBytes = null;
        infoPanel.SetActive(false);
    }



    /*

        * 갤러리 기능

        ImageLoadButtonClick() - 갤러리로부터 이미지 가져옴
        GetImage() - 이미지 저장

    */

    public void ImageLoadButtonClick()
    {
        NativeGallery.GetImageFromGallery((file) =>
        {
            FileInfo selected = new FileInfo(file);

            if (selected.Length > 50000000)
            {
                return;
            }

            if (!string.IsNullOrEmpty(file))
            {
                StartCoroutine(GetImage(file));
            }
        });

    }

    IEnumerator GetImage(string path)
    {
        yield return null;

        byte[] fileData = File.ReadAllBytes(path);

        if (tempImage != null)
        {
            Destroy(tempImage);
        }

        Texture2D tex = new Texture2D(0, 0);
        tex.LoadImage(fileData);

        tempImage = tex;
        tempBytes = fileData;
        img.texture = tempImage;  
    }



    /*

        * 카메라 기능

        CameraOnButtonClick() - '촬영' 버튼 클릭 시 카메라 권한 세팅
        ActivateCamer() - 후면 카메라 탐색
        TakeAPhoto() - 셔터 버튼 클릭 시 함수 실행
        Rendering() - 스크린 캡쳐 후 상세 창의 이미지에 반영
        CameraOff() - 카메라 창 종료

    */

    public void CameraOnButtonClick()
    {
        Color color = infoPanel.GetComponent<Image>().color;
        color.a = 1f;
        infoPanel.GetComponent<Image>().color = color;

        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            PermissionCallbacks permissionCallbacks = new();
            permissionCallbacks.PermissionGranted += ActivateCamera;
            Permission.RequestUserPermission(Permission.Camera, permissionCallbacks);
        }
        else
        {
            ActivateCamera();
        }
    }

    private void ActivateCamera(string permissionName = null)
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0) return;

        int selectedCamIdx = -1;

        for (int i = 0; i < devices.Length; i++)
        {
            if (!devices[i].isFrontFacing)
            {
                selectedCamIdx = i;
                break;
            }
        }

        if (selectedCamIdx != -1)
        {
            camTexture = new WebCamTexture(devices[selectedCamIdx].name);
            camTexture.requestedFPS = 30;
            camImg.texture = camTexture;
            camTexture.Play();
        }
    }

    public void TakeAPhoto()
    {
        StartCoroutine(Rendering());
    }

    IEnumerator Rendering()
    {
        tapButton.gameObject.SetActive(false);

        yield return new WaitForEndOfFrame();

        Texture2D texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
        texture.Apply();

        tempBytes = texture.EncodeToPNG();
        tempImage = texture;
        img.texture = tempImage;

        yield return CameraOff();
    }

    IEnumerator CameraOff()
    {
        yield return new WaitForEndOfFrame();

        if (camTexture != null)
        {
            camTexture.Stop();
            Destroy(camTexture);
            camTexture = null;
        }

        camImg.gameObject.SetActive(false);

        Color color = infoPanel.GetComponent<Image>().color;
        color.a = 0.4f;
        infoPanel.GetComponent<Image>().color = color;
    }
}
