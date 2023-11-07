using UnityEngine;
using System.Collections;

public class UrgDevice : MonoBehaviour {

	public enum CMD
	{
		// https://www.hokuyo-aut.jp/02sensor/07scanner/download/pdf/URG_SCIP20.pdf
		VV, PP, II, // 센서 정보 요구 명령어(3종류)
        BM, QT, // 계측 시작 및 종료 명령어
        MD, GD, // 거리 요구 명령어(2종류)
        ME // 거리 및 수광 강도 요구 명령어
    }

	public static string GetCMDString(CMD cmd)
	{
		return cmd.ToString();
	}
}
