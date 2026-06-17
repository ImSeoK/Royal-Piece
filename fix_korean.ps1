[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Fix($path, $replacements) {
    $c = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    foreach ($r in $replacements) {
        $c = [regex]::Replace($c, $r[0], $r[1])
    }
    [System.IO.File]::WriteAllText($path, $c, [System.Text.Encoding]::UTF8)
    Write-Host "Fixed: $([System.IO.Path]::GetFileName($path))"
}

$attrR = @(
    ,@('if \(unit\.isKing\) return "[^"]*"',           'if (unit.isKing) return "킹"')
    ,@('if \(unit\.isPawn\) return "[^"]*"',            'if (unit.isPawn) return "폰"')
    ,@('if \(hasRook && hasBishop\) return "[^"]*"',    'if (hasRook && hasBishop) return "퀸"')
    ,@('if \(hasRook\) return "[^"]*"',                 'if (hasRook) return "룩"')
    ,@('if \(hasBishop\) return "[^"]*"',               'if (hasBishop) return "비숍"')
    ,@('if \(hasKnight\) return "[^"]*"',               'if (hasKnight) return "나이트"')
)
$rarityR = @(
    ,@('case UnitRarity\.Common: return "[^"]*"',       'case UnitRarity.Common: return "커먼"')
    ,@('case UnitRarity\.Rare: return "[^"]*"',         'case UnitRarity.Rare: return "레어"')
    ,@('case UnitRarity\.Epic: return "[^"]*"',         'case UnitRarity.Epic: return "에픽"')
    ,@('case UnitRarity\.Legendary: return "[^"]*"',    'case UnitRarity.Legendary: return "레전더리"')
)

# ─── InventoryCard.cs ───────────────────────────────────────────────────────
Fix 'Assets/Project/Presentation/InventoryCard.cs' (@(
    # stat labels (2 spaces before interpolation)
    ,@('(atkText\.text = \$)"[^"]*  (\{unit\.GetEnhancedAttack[^}]*\})"', '$1"공격  $2"')
    ,@('(spdText\.text = \$)"[^"]*  (\{unit\.GetEnhancedSpeed[^}]*\})"',  '$1"속도  $2"')
    ,@('(defText\.text = \$)"[^"]*  (\{unit\.GetEnhancedDefense[^}]*\})"','$1"방어력  $2"')
    ,@('(atkText\.text = \$)"[^"]*  (\{unit\.attackPower\})"',             '$1"공격  $2"')
    ,@('(spdText\.text = \$)"[^"]*  (\{unit\.speed\})"',                  '$1"속도  $2"')
    ,@('(defText\.text = \$)"[^"]*  (\{unit\.defense\})"',                '$1"방어력  $2"')
) + $attrR)

# ─── UnitInfoPanel.cs ───────────────────────────────────────────────────────
Fix 'Assets/Project/Presentation/UnitInfoPanel.cs' (@(
    ,@('labelText\.text = "[^"]*"',                                         'labelText.text = "아군"')
    ,@('(atkText\.text = \$)"[^"]* (\{unit\.definition\.attackPower\})"',  '$1"공격 $2"')
    ,@('(spdText\.text = \$)"[^"]* (\{unit\.definition\.speed\})"',        '$1"속도 $2"')
    ,@('(defText\.text = \$)"[^"]* (\{unit\.GetDefense\(\)\})"',           '$1"방어력 $2"')
) + $attrR)

# ─── EnemyUnitInfoPanel.cs ──────────────────────────────────────────────────
Fix 'Assets/Project/Presentation/EnemyUnitInfoPanel.cs' (@(
    ,@('labelText\.text = "[^"]*"',                                         'labelText.text = "적군"')
    ,@('(atkText\.text = \$)"[^"]* (\{unit\.definition\.attackPower\})"',  '$1"공격 $2"')
    ,@('(spdText\.text = \$)"[^"]* (\{unit\.definition\.speed\})"',        '$1"속도 $2"')
    ,@('(defText\.text = \$)"[^"]* (\{unit\.GetDefense\(\)\})"',           '$1"방어력 $2"')
) + $attrR)

# ─── UnitDetailPopup.cs ─────────────────────────────────────────────────────
Fix 'Assets/Project/Presentation/UnitDetailPopup.cs' (@(
    ,@('activeSkillNameText\.text = "[^"]*"',   'activeSkillNameText.text = "액티브 스킬 없음"')
    ,@('texts\[0\]\.text = "[^"]*"',            'texts[0].text = "패시브 스킬 없음"')
) + $attrR + $rarityR)

# ─── EnhancePopup.cs ────────────────────────────────────────────────────────
Fix 'Assets/Project/Presentation/EnhancePopup.cs' (@(
    ,@('(hpAfterText\.text = \$)"[^"]* (\{targetDef\.GetEnhancedHP[^}]*\})"',     '$1"→ $2"')
    ,@('(atkAfterText\.text = \$)"[^"]* (\{targetDef\.GetEnhancedAttack[^}]*\})"','$1"→ $2"')
    ,@('(spdAfterText\.text = \$)"[^"]* (\{targetDef\.GetEnhancedSpeed[^}]*\})"', '$1"→ $2"')
    ,@('(defAfterText\.text = \$)"[^"]* (\{targetDef\.GetEnhancedDefense[^}]*\})"','$1"→ $2"')
    ,@('hpAfterText\.text = "[^"]*"',   'hpAfterText.text = "→ ?"')
    ,@('atkAfterText\.text = "[^"]*"',  'atkAfterText.text = "→ ?"')
    ,@('spdAfterText\.text = "[^"]*"',  'spdAfterText.text = "→ ?"')
    ,@('defAfterText\.text = "[^"]*"',  'defAfterText.text = "→ ?"')
) + $attrR + $rarityR)

# ─── UnitDefinition.cs  (Header only) ───────────────────────────────────────
Fix 'Assets/Project/Core/UnitDefinition.cs' @(
    ,@('(\[Header\(")[^"]*("\)\]\r?\n\s*public string unitName)',     '${1}기본 정보${2}')
    ,@('(\[Header\(")[^"]*("\)\]\r?\n\s*public int maxHP)',           '${1}기본 스탯${2}')
    ,@('(\[Header\(")[^"]*("\)\]\r?\n\s*public MovementAttribute)',   '${1}이동 속성${2}')
    ,@('(\[Header\(")[^"]*("\)\]\r?\n\s*public bool isKing)',         '${1}특수 속성${2}')
    ,@('(\[Header\(")[^"]*("\)\]\r?\n\s*public UnitRarity)',          '${1}희귀 등급${2}')
    ,@('(\[Header\(")[^"]*("\)\]\r?\n\s*public SkillDefinition)',     '${1}스킬${2}')
)

# ─── SkillDefinition.cs  (Header only) ──────────────────────────────────────
Fix 'Assets/Project/Core/SkillDefinition.cs' @(
    ,@('(\[Header\(")[^"]*("\)\]\r?\n\s*public string skillName)',        '${1}기본 정보${2}')
    ,@('(\[Header\(")[^"]*("\)\]\r?\n\s*public SkillType)',               '${1}스킬 분류${2}')
    ,@('(\[Header\(")[^"]*("\)\]\r?\n\s*public SkillTargetType)',         '${1}효과${2}')
    ,@('(\[Header\(")[^"]*("\)\]\r?\n\s*public StatType)',                '${1}버프 설정${2}')
    ,@('(\[Header\(")[^"]*("\)\]\r?\n\s*public int maxUseCount)',         '${1}액티브 설정${2}')
    ,@('(\[Header\(")[^"]*("\)\]\r?\n\s*public GameObject castEffect)',   '${1}이펙트 (선택 사항)${2}')
)

# ─── GachaManager.cs  (Debug message) ───────────────────────────────────────
Fix 'Assets/Project/Presentation/GachaManager.cs' @(
    ,@('"\[GachaManager\][^"]*"', '"[GachaManager] GachaSystem을 찾을 수 없습니다!"')
)

# ─── PickerCard.cs  (Header only) ───────────────────────────────────────────
Fix 'Assets/Project/Presentation/PickerCard.cs' @(
    ,@('(\[Header\(")[^"]*("\)\]\r?\n\s*public Sprite badgeCommon)', '${1}뱃지 스프라이트${2}')
)

# ─── MoveEffect.cs  (Comment only – no runtime impact, skip) ────────────────
Write-Host "Done."
