using System.Collections.Generic;

/// <summary>
/// 장착 칸의 포켓몬 성을 무기 능력 레벨로 바꾼다.
/// </summary>
internal static class EquippedAbilityResolver
{
    /// <summary>
    /// 성을 무기 능력 레벨 인덱스로 바꾼다. 1성은 0번이다.
    /// </summary>
    public static int ToAbilityLevel(int star, WeaponAbilityData ability)
    {
        var index = star - Item.STAR_MIN;
        if (index < 0)
        {
            index = 0;
        }

        if (ability == null || ability.LevelsCount <= 0)
        {
            return 0;
        }

        var last = ability.LevelsCount - 1;
        return index > last ? last : index;
    }

    /// <summary>
    /// 점 발사 프리팹이면 true. 칸마다 따로 둔다.
    /// </summary>
    public static bool IsSlotOriginAbility(WeaponAbilityData ability)
    {
        return ability != null
            && ability.Prefab != null
            && ability.Prefab.GetComponent<ISlotOriginAbility>() != null;
    }

    /// <summary>
    /// 장착 중인 같은 능력 중 가장 높은 성에 해당하는 레벨 인덱스. 없으면 0.
    /// </summary>
    public static int GetLevel(InventoryHolder equipment, ItemCatalog catalog, WeaponAbilityData ability)
    {
        var star = GetHighestStar(equipment, catalog, ability);
        if (star <= 0)
        {
            return 0;
        }

        return ToAbilityLevel(star, ability);
    }

    /// <summary>
    /// 장착된 포켓몬의 능력을 모은다. 점 발사는 칸마다, 그 외는 타입에서 가장 높은 성이다.
    /// </summary>
    public static void Collect(InventoryHolder equipment, ItemCatalog catalog, List<EquippedAbilityLevel> results)
    {
        results.Clear();
        if (equipment == null)
        {
            return;
        }

        for (var i = 0; i < equipment.Stacks.Count; i++)
        {
            var stack = equipment.Stacks[i];
            if (stack.Empty || stack.Item == null)
            {
                continue;
            }

            var visual = catalog.GetVisual(stack.Item.uid);
            var ability = visual != null ? visual.WeaponAbility : null;
            if (ability == null)
            {
                continue;
            }

            var level = ToAbilityLevel(stack.Item.upgradeLevel, ability);
            if (IsSlotOriginAbility(ability))
            {
                results.Add(new EquippedAbilityLevel(ability, level, i));
                continue;
            }

            var found = false;
            for (var u = 0; u < results.Count; u++)
            {
                if (results[u].Slot != EquippedAbilityLevel.UNSLOTTED
                    || results[u].Ability.WeaponAbilityType != ability.WeaponAbilityType)
                {
                    continue;
                }

                found = true;
                if (level > results[u].LevelIndex)
                {
                    results[u] = new EquippedAbilityLevel(ability, level, EquippedAbilityLevel.UNSLOTTED);
                }

                break;
            }

            if (!found)
            {
                results.Add(new EquippedAbilityLevel(ability, level, EquippedAbilityLevel.UNSLOTTED));
            }
        }
    }

    private static int GetHighestStar(InventoryHolder equipment, ItemCatalog catalog, WeaponAbilityData ability)
    {
        if (equipment == null || ability == null)
        {
            return 0;
        }

        var star = 0;
        for (var i = 0; i < equipment.Stacks.Count; i++)
        {
            var stack = equipment.Stacks[i];
            if (stack.Empty || stack.Item == null || stack.Item.upgradeLevel <= star)
            {
                continue;
            }

            var visual = catalog.GetVisual(stack.Item.uid);
            if (visual == null || visual.WeaponAbility == null)
            {
                continue;
            }

            if (visual.WeaponAbility.WeaponAbilityType == ability.WeaponAbilityType)
            {
                star = stack.Item.upgradeLevel;
            }
        }

        return star;
    }
}
