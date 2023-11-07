using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// http://sourceforge.net/p/urgnetwork/wiki/top_jp/
// https://www.hokuyo-aut.co.jp/02sensor/07scanner/download/pdf/URG_SCIP20.pdf
public class Sensor : MonoBehaviour
{
    class DetectObject
    {
        public List<long> distList;
        public List<int> idList;

        public long startDist;

        public DetectObject()
        {
            distList = new List<long>();
            idList = new List<int>();
        }
    }

    [SerializeField]
    string ip_address = "192.168.0.180";
    [SerializeField]
    int port_number = 10940;

    List<DetectObject> detectObjects;
    List<int> detectIdList;


    UrgDeviceEthernet urg;
    public int sensorScanSteps { get; private set; }
    public float scale = 0.1f;

    public Color distanceColor = Color.white;

    public Color[] groupColors;

    List<long> distances;

    private Vector3[] directions;
    long[] distanceConstrainList;

    
    public Vector2 resolution;

    public Vector2 pivot;
    public Rect areaRect;

    public bool debugDraw = false;



    void CalculateDistanceConstrainList(int steps)
    {
        float keyAngle = Mathf.Atan(areaRect.height / (areaRect.width / 2f));

        for (int i = 0; i < steps; i++)
        {
            if (directions[i].y <= 0)
            {
                distanceConstrainList[i] = 0;
            }
            else
            {
                ;

                float a = Vector3.Angle(directions[i], Vector3.right) * Mathf.Deg2Rad;
                float tanAngle = Mathf.Tan(a);
                float pn = tanAngle / Mathf.Abs(tanAngle);

                float r = 0;
                if (a < keyAngle || a > Mathf.PI - keyAngle)
                {
                    float x = pn * areaRect.width / 2;
                    float y = x * Mathf.Tan(a);
                    r = y / Mathf.Sin(a);
                }
                else if (a >= keyAngle && a <= Mathf.PI - keyAngle)
                {
                    float angle2 = Mathf.PI / 2 - a;
                    float y = areaRect.height;
                    float x = y * Mathf.Tan(angle2);
                    r = x / Mathf.Sin(angle2);
                }

                if (r < 0 || float.IsNaN(r))
                {
                    r = 0;
                }

                distanceConstrainList[i] = (long)r;
            }


        }
    }
    List<long> ConstrainDetectionArea(List<long> beforeCrop)
    {
        List<long> result = new List<long>();

        for (int i = 0; i < beforeCrop.Count; i++)
        {
            if (beforeCrop[i] > distanceConstrainList[i] || beforeCrop[i] <= 0)
            {
                result.Add(distanceConstrainList[i]);
            }
            else
            {
                result.Add(beforeCrop[i]);
            }
        }

        return result;
    }
    private void CacheDirections()
    {
        float d = Mathf.PI * 2 / 1440;
        float offset = d * 540;
        directions = new Vector3[sensorScanSteps];
        for (int i = 0; i < directions.Length; i++)
        {
            float a = d * i + offset;
            directions[i] = new Vector3(-Mathf.Cos(a), -Mathf.Sin(a), 0);
        }
    }

    // Use this for initialization
    void Start()
    {
        distances = new List<long>();

        detectObjects = new List<DetectObject>();


        urg = this.gameObject.AddComponent<UrgDeviceEthernet>();
        urg.StartTCP(ip_address, port_number);

        urg.Write(SCIP_library.SCIP_Writer.MD(0, 1080, 1, 0, 0));
    }

    // Update is called once per frame
    void Update()
    {
        // center offset rect 사용 안됨
        Rect detectAreaRect = areaRect;
        detectAreaRect.x *= scale;
        detectAreaRect.y *= scale;
        detectAreaRect.width *= scale;
        detectAreaRect.height *= scale;

        detectAreaRect.x = -detectAreaRect.width / 2;
        //

        //Setting up things, one time
        if (sensorScanSteps <= 0)
        {
            sensorScanSteps = urg.distances.Count;
            distanceConstrainList = new long[sensorScanSteps];
            CacheDirections();
            CalculateDistanceConstrainList(sensorScanSteps);

        }

        // distances
        try
        {
            if (urg.distances.Count > 0)
            {
                distances.Clear();
                distances.AddRange(ConstrainDetectionArea(urg.distances));
            }
        }
        catch
        {
        }

        if (debugDraw)
        {
            for (int i = 0; i < distances.Count; i++)
            {
                Vector3 dir = directions[i];
                long dist = distances[i];
                Debug.DrawRay(Vector3.zero, dist * dir * scale, distanceColor);
            }

            DrawRect(detectAreaRect, Color.green);
        }
    }
    void DrawRect(Rect rect, Color color)
    {
        Vector3 p0 = new Vector3(rect.x, rect.y, 0);
        Vector3 p1 = new Vector3(rect.x + rect.width, rect.y, 0);
        Vector3 p2 = new Vector3(rect.x + rect.width, rect.y + rect.height, 0);
        Vector3 p3 = new Vector3(rect.x, rect.y + rect.height, 0);
        Debug.DrawLine(p0, p1, color);
        Debug.DrawLine(p1, p2, color);
        Debug.DrawLine(p2, p3, color);
        Debug.DrawLine(p3, p0, color);
    }

    //	운전모드 설정
    //		"SCIP 2.0"...SCIP2.0 모드 변경
    //		"TM0", "TM1", "TM2"...타임스탬프 모드
    //		"SS"...보레이트 변경
    //		"BM"...레이저의 방사.
    //		"QT"...레이저를 끄면 측정이 정지됩니다.
    //		"RS"...파라미터를 재설정합니다. 측정이 일시 중단됩니다.
    //		"CR"...모터의 회전 속도를 변경합니다.
    //	상태 정보 가져오기
    //		"VV"...버전 정보 획득.
    //		"PP"...파라미터 정보의 획득.
    //		"II"...신분 취득.
    //	거리 데이터 수신
    //		"GD", "GS"...거리 데이터를 한 번에 하나씩 획득합니다.
    //		"MD", "MS"...거리 데이터를 연속적으로 획득합니다.

    // PP
    //	MODL ... 센서 형식 정보
    //	DMIN ... 최소 계측 가능 거리 (mm)
    //	DMAX ... 최대 계측 가능 거리 (mm)
    //	ARES ... 각도 분해능(360도 분할수)
    //	AMIN ... 최소 계측 가능 방향값
    //	AMAX ... 최대 계측 가능 방향값
    //	AFRT ... 정면 방향값
    //	SCAN ... 표준 조작각속도

    void OnGUI()
    {
        // https://sourceforge.net/p/urgnetwork/wiki/scip_jp/
        if (GUILayout.Button("VV: (버전 정보 취득)"))
        {
            urg.Write(SCIP_library.SCIP_Writer.VV());
        }
        //		if(GUILayout.Button("SCIP2")){
        //			urg.Write(SCIP_library.SCIP_Writer.SCIP2());
        //		}
        if (GUILayout.Button("PP: (파라미터 정보의 취득)"))
        {
            urg.Write(SCIP_library.SCIP_Writer.PP());
        }
        if (GUILayout.Button("MD: (계측&송신 요구)"))
        {
            urg.Write(SCIP_library.SCIP_Writer.MD(0, 1080, 1, 0, 0));
        }
        if (GUILayout.Button("ME: (계측&거리 데이터·수광 강도값 송신 요구)"))
        {
            urg.Write(SCIP_library.SCIP_Writer.ME(0, 1080, 1, 1, 0));
        }
        if (GUILayout.Button("BM: (레이저 발광)"))
        {
            urg.Write(SCIP_library.SCIP_Writer.BM());
        }
        if (GUILayout.Button("GD: (계측된 거리 데이터 전송 요구)"))
        {
            urg.Write(SCIP_library.SCIP_Writer.GD(0, 1080));
        }
        if (GUILayout.Button("QUIT"))
        {
            urg.Write(SCIP_library.SCIP_Writer.QT());
        }
    }


}
