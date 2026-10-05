using UnityEngine;
using System;

public class MasterDataResponse
{
    public string version;
    public MasterDataPayload data;
}

/// <summary>
/// 실제 MasterData 항목.
/// 현재 서버 응답이 빈 객체 {}이므로 필드는 아직 정의하지 않는다.
/// </summary>
[Serializable]
public class MasterDataPayload
{
}