using System;
using UnityEngine;

/// <summary>
/// 상점 칸을 뽑는 확률. 플레이어 레벨 구간마다 종족값 등급과 성의 가중치를 둔다.
/// 등급은 종족값 경계로 나눈 일반 등급들과, 종족값과 상관없는 전설 등급 하나다.
/// </summary>
[CreateAssetMenu(fileName = "ShopOddsSettings", menuName = "Inventory/Shop Odds Settings")]
public class ShopOddsSettings : ScriptableObject
{
    /// <summary>
    /// 일반 등급 수. 종족값 경계는 이보다 하나 적다.
    /// </summary>
    public const int NORMAL_TIER_COUNT = 4;
    public const int LEGENDARY_TIER = NORMAL_TIER_COUNT;
    public const int TIER_COUNT = NORMAL_TIER_COUNT + 1;

    /// <summary>
    /// minLevel 이상인 레벨에 쓰는 가중치. 가중치는 합이 아니라 비율이다.
    /// </summary>
    [Serializable]
    public class LevelBracket
    {
        [SerializeField, Min(1)] private int _minLevel = 1;
        [Tooltip("T1, T2, T3, T4, 전설 순서")]
        [SerializeField] private float[] _tierWeights = new float[TIER_COUNT];
        [Tooltip("1성, 2성, 3성 순서")]
        [SerializeField] private float[] _starWeights = new float[Item.STAR_MAX];

        public LevelBracket()
        {
        }

        public LevelBracket(int minLevel, float[] tierWeights, float[] starWeights)
        {
            _minLevel = minLevel;
            _tierWeights = tierWeights;
            _starWeights = starWeights;
        }

        public int MinLevel => _minLevel;

        /// <summary>
        /// 등급의 가중치. 비었거나 음수면 0.
        /// </summary>
        public float GetTierWeight(int tier)
        {
            return GetWeight(_tierWeights, tier);
        }

        /// <summary>
        /// star성의 가중치. 비었거나 음수면 0.
        /// </summary>
        public float GetStarWeight(int star)
        {
            return GetWeight(_starWeights, star - Item.STAR_MIN);
        }

        private static float GetWeight(float[] weights, int index)
        {
            if (weights == null || index < 0 || index >= weights.Length)
            {
                return 0f;
            }

            return weights[index] > 0f ? weights[index] : 0f;
        }
    }

    [Tooltip("일반 등급을 나누는 종족값 경계. 오름차순. 첫 값 미만이 T1이다")]
    [SerializeField] private int[] _tierThresholds = { 320, 400, 480 };

    [Tooltip("minLevel 오름차순. 현재 레벨 이하에서 가장 큰 minLevel 구간을 쓴다")]
    [SerializeField] private LevelBracket[] _brackets =
    {
        new LevelBracket(1, new float[] { 70, 25, 5, 0, 0 }, new float[] { 100, 0, 0 }),
        new LevelBracket(10, new float[] { 55, 30, 13, 2, 0 }, new float[] { 95, 5, 0 }),
        new LevelBracket(20, new float[] { 45, 33, 17, 5, 0 }, new float[] { 90, 10, 0 }),
        new LevelBracket(30, new float[] { 35, 35, 22, 8, 0 }, new float[] { 85, 14, 1 }),
        new LevelBracket(40, new float[] { 28, 33, 27, 11, 1 }, new float[] { 80, 18, 2 }),
        new LevelBracket(50, new float[] { 22, 30, 30, 16, 2 }, new float[] { 74, 22, 4 }),
        new LevelBracket(60, new float[] { 17, 27, 32, 21, 3 }, new float[] { 68, 26, 6 }),
        new LevelBracket(70, new float[] { 13, 24, 33, 26, 4 }, new float[] { 62, 30, 8 }),
        new LevelBracket(80, new float[] { 10, 20, 33, 31, 6 }, new float[] { 55, 34, 11 }),
        new LevelBracket(100, new float[] { 8, 17, 30, 37, 8 }, new float[] { 45, 40, 15 }),
    };

    [Tooltip("가방이나 장착 칸에 있는 계통의 기본형 가중치 배율")]
    [SerializeField, Min(1f)] private float _ownedWeightMultiplier = 1.5f;

    [Tooltip("켜면 전설 등급은 늘 1성으로 나온다")]
    [SerializeField] private bool _legendaryAlwaysOneStar = true;

    public float OwnedWeightMultiplier => _ownedWeightMultiplier < 1f ? 1f : _ownedWeightMultiplier;

    public bool LegendaryAlwaysOneStar => _legendaryAlwaysOneStar;

    /// <summary>
    /// 종의 상점 등급. 전설이면 LEGENDARY_TIER, 아니면 종족값 경계를 넘은 수다.
    /// </summary>
    public int GetTier(MonsterVisualData visual)
    {
        if (visual == null)
        {
            return 0;
        }

        if (visual.Legendary)
        {
            return LEGENDARY_TIER;
        }

        var tier = 0;
        if (_tierThresholds != null)
        {
            for (var i = 0; i < _tierThresholds.Length; i++)
            {
                if (visual.BaseStatTotal >= _tierThresholds[i])
                {
                    tier++;
                }
            }
        }

        return tier < NORMAL_TIER_COUNT ? tier : NORMAL_TIER_COUNT - 1;
    }

    /// <summary>
    /// 레벨에 맞는 구간. 구간이 없으면 null.
    /// </summary>
    public LevelBracket GetBracket(int level)
    {
        LevelBracket found = null;
        if (_brackets == null)
        {
            return null;
        }

        for (var i = 0; i < _brackets.Length; i++)
        {
            var bracket = _brackets[i];
            if (bracket == null || bracket.MinLevel > level)
            {
                continue;
            }

            if (found == null || bracket.MinLevel > found.MinLevel)
            {
                found = bracket;
            }
        }

        if (found != null)
        {
            return found;
        }

        // 레벨이 모든 구간보다 낮으면 가장 낮은 구간을 쓴다.
        for (var i = 0; i < _brackets.Length; i++)
        {
            if (_brackets[i] != null && (found == null || _brackets[i].MinLevel < found.MinLevel))
            {
                found = _brackets[i];
            }
        }

        return found;
    }
}
