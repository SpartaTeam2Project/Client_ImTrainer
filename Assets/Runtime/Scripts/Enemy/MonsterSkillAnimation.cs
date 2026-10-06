using System;
using UnityEngine;

/// <summary>
/// 이름이 붙은 특수 기술 그림 하나. 이름은 SpriteCollab 애니 이름 그대로다(예: Shock, QuickStrike, Twirl).
/// </summary>
[Serializable]
public class MonsterSkillAnimation
{
    [SerializeField] private string _name = string.Empty;
    [SerializeField] private EightDirectionFrames _frames = new EightDirectionFrames();

    public string Name => _name ?? string.Empty;

    public EightDirectionFrames Frames => _frames;
}
