param(
    [string]$PromptPath = (Join-Path $PSScriptRoot 'wireframe-prompts.json'),
    [string]$OutputDirectory = $PSScriptRoot
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$catalog = Get-Content -LiteralPath $PromptPath -Raw | ConvertFrom-Json
$width = [int]$catalog.resolution.width
$height = [int]$catalog.resolution.height
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

function New-WireColor([string]$hex, [int]$alpha = 255) {
    $value = $hex.TrimStart('#')
    return [System.Drawing.Color]::FromArgb(
        $alpha,
        [Convert]::ToInt32($value.Substring(0, 2), 16),
        [Convert]::ToInt32($value.Substring(2, 2), 16),
        [Convert]::ToInt32($value.Substring(4, 2), 16))
}

function Use-Pen([string]$hex, [int]$alpha, [float]$width, [scriptblock]$body) {
    $pen = New-Object System.Drawing.Pen (New-WireColor $hex $alpha), $width
    try { & $body $pen } finally { $pen.Dispose() }
}

function Use-Brush([string]$hex, [int]$alpha, [scriptblock]$body) {
    $brush = New-Object System.Drawing.SolidBrush (New-WireColor $hex $alpha)
    try { & $body $brush } finally { $brush.Dispose() }
}

function New-WireFont([float]$size, [System.Drawing.FontStyle]$style = [System.Drawing.FontStyle]::Regular) {
    return New-Object System.Drawing.Font 'Consolas', $size, $style, ([System.Drawing.GraphicsUnit]::Pixel)
}

function Draw-Rect($graphics, [int]$x, [int]$y, [int]$w, [int]$h, [string]$stroke = '#9bdc65', [string]$fill = '#05070c', [int]$alpha = 170, [float]$thickness = 1) {
    Use-Brush $fill $alpha { param($brush) $graphics.FillRectangle($brush, $x, $y, $w, $h) }
    Use-Pen $stroke 230 $thickness { param($pen) $graphics.DrawRectangle($pen, $x, $y, $w, $h) }
}

function Draw-Line($graphics, [int]$x1, [int]$y1, [int]$x2, [int]$y2, [string]$stroke = '#9bdc65', [int]$alpha = 190, [float]$thickness = 1) {
    Use-Pen $stroke $alpha $thickness { param($pen) $graphics.DrawLine($pen, $x1, $y1, $x2, $y2) }
}

function Draw-Text($graphics, [string]$text, [int]$x, [int]$y, [int]$w, [int]$h, [float]$size = 20, [string]$color = '#d8d8d8', [string]$align = 'Near') {
    $font = New-WireFont $size
    $format = New-Object System.Drawing.StringFormat
    try {
        $format.Alignment = [System.Drawing.StringAlignment]::$align
        $format.LineAlignment = [System.Drawing.StringAlignment]::Near
        $format.Trimming = [System.Drawing.StringTrimming]::EllipsisWord
        Use-Brush $color 240 {
            param($brush)
            $graphics.DrawString($text, $font, $brush, [System.Drawing.RectangleF]::new($x, $y, $w, $h), $format)
        }
    }
    finally {
        $format.Dispose()
        $font.Dispose()
    }
}

function Draw-Button($graphics, [string]$label, [int]$x, [int]$y, [int]$w, [int]$h, [string]$fill = '#15191f') {
    Draw-Rect $graphics $x $y $w $h '#76806f' $fill 210 1
    Draw-Text $graphics $label ($x + 8) ($y + 12) ($w - 16) ($h - 12) 20 '#d8d8d8' 'Center'
}

function Draw-Starfield($graphics, [int]$seed) {
    $graphics.Clear((New-WireColor '#05070c'))
    $random = [System.Random]::new($seed)
    for ($i = 0; $i -lt 190; $i++) {
        $x = $random.Next(0, $width)
        $y = $random.Next(0, $height)
        $s = $random.Next(1, 3)
        $alpha = $random.Next(80, 210)
        Use-Brush '#d8e8ff' $alpha { param($brush) $graphics.FillEllipse($brush, $x, $y, $s, $s) }
    }
}

function Draw-InteractionStarMapBackdrop($graphics) {
    $graphics.Clear((New-WireColor '#04070d'))
    Use-Brush '#07111c' 210 { param($brush) $graphics.FillRectangle($brush, 0, 0, $width, $height) }

    for ($i = 0; $i -lt 20; $i++) {
        $y = 610 + ($i * 11)
        $alpha = [Math]::Max(0, 58 - ($i * 2))
        Use-Brush '#3a152d' $alpha { param($brush) $graphics.FillRectangle($brush, 0, $y, $width, 14) }
    }

    for ($i = 0; $i -lt 18; $i++) {
        $alpha = [Math]::Max(0, 52 - ($i * 2))
        Use-Brush '#0b2036' $alpha {
            param($brush)
            $graphics.FillRectangle($brush, 0, $i * 18, 72, $height)
            $graphics.FillRectangle($brush, $width - 72, $i * 18, 72, $height)
        }
    }

    $random = [System.Random]::new(20260429)
    for ($i = 0; $i -lt 430; $i++) {
        $x = $random.Next(0, $width)
        $y = $random.Next(0, $height)
        $s = $random.Next(1, 3)
        $alpha = $random.Next(55, 205)
        Use-Brush '#d8e8ff' $alpha { param($brush) $graphics.FillEllipse($brush, $x, $y, $s, $s) }
    }

    for ($i = 0; $i -lt 58; $i++) {
        $x = $random.Next(0, $width)
        $y = $random.Next(0, $height)
        $r = $random.Next(5, 12)
        $halo = $r * 3
        $alpha = $random.Next(18, 44)
        $color = if (($i % 5) -eq 0) { '#d8b77b' } elseif (($i % 3) -eq 0) { '#96c8ff' } else { '#d8e8ff' }
        Use-Brush $color $alpha { param($brush) $graphics.FillEllipse($brush, $x - $halo, $y - $halo, $halo * 2, $halo * 2) }
        Use-Brush $color ([Math]::Min(230, $alpha * 4)) { param($brush) $graphics.FillEllipse($brush, $x - 2, $y - 2, 4, 4) }
    }
}

function Draw-MenuBullet($graphics, [string]$text, [int]$x, [int]$y, [int]$w) {
    Draw-Text $graphics 'SYS' $x $y 48 28 18 '#9bdc65'
    Draw-Text $graphics $text ($x + 62) $y ($w - 62) 28 18 '#d8d8d8'
}

function Draw-MapSystems($graphics) {
    Draw-Line $graphics 500 310 1030 590 '#a6a6a6' 180 2
    Draw-Line $graphics 1030 590 1235 490 '#a6a6a6' 160 2
    $systems = @(
        @{ X = 420; Y = 225; R = 82; Name = 'Sirius' },
        @{ X = 770; Y = 430; R = 92; Name = 'Arcturus' },
        @{ X = 1180; Y = 610; R = 84; Name = 'Canopus' }
    )
    foreach ($system in $systems) {
        Use-Pen '#ffff00' 245 4 { param($pen) $graphics.DrawEllipse($pen, $system.X - $system.R, $system.Y - $system.R, $system.R * 2, $system.R * 2) }
        Use-Pen '#405040' 150 1 { param($pen)
            $graphics.DrawEllipse($pen, $system.X - 42, $system.Y - 42, 84, 84)
            $graphics.DrawEllipse($pen, $system.X - 58, $system.Y - 58, 116, 116)
        }
        Use-Brush '#82b7e4' 240 { param($brush) $graphics.FillEllipse($brush, $system.X - 30, $system.Y - 18, 34, 34) }
        Use-Brush '#ffdd00' 245 { param($brush) $graphics.FillEllipse($brush, $system.X + 16, $system.Y + 2, 18, 18) }
        Use-Brush '#caa06d' 245 { param($brush) $graphics.FillEllipse($brush, $system.X + 46, $system.Y + 20, 42, 42) }
        Draw-Text $graphics $system.Name ($system.X - 50) ($system.Y + $system.R + 10) 120 28 18 '#e0e0e0' 'Center'
    }
}

function Draw-GameplayShell($graphics, $screen) {
    Draw-Starfield $graphics ([Math]::Abs($screen.id.GetHashCode()))
    Draw-Rect $graphics 0 0 $width 126 '#9bdc65' '#111611' 210 2
    Draw-Text $graphics 'Turn 1 | Production Phase | Active: Cadet' 20 18 520 34 22 '#d0d0d0'
    Draw-Button $graphics 'POP 100' 20 74 108 44 '#15191f'
    Draw-Button $graphics 'MET 50' 142 74 100 44 '#15191f'
    Draw-Button $graphics 'FUEL 50' 256 74 106 44 '#15191f'
    Draw-Text $graphics 'Panels F1 Dbg:On F2 Cmd:Off F3 AI:Off F4 UI:Off F5 Ref:Off F6 Tut:Off H Help' 975 64 540 30 18 '#9bdc65'
    Draw-Rect $graphics 0 126 268 ($height - 126) '#1b2328' '#151c21' 245 1
    Draw-Rect $graphics 1300 126 236 ($height - 126) '#1b2328' '#151c21' 245 1
    Draw-Rect $graphics 1350 180 170 180 '#9bdc65' '#05070c' 200 1
    Draw-Text $graphics 'Selected Region' 1368 198 148 34 22 '#d8d8d8'
    Draw-Rect $graphics 1350 395 170 250 '#9bdc65' '#05070c' 200 1
    Draw-Text $graphics 'Map Key' 1368 415 148 34 22 '#d8d8d8'
    Draw-MapSystems $graphics
}

function Draw-CommandDeck($graphics, $screen) {
    Draw-Starfield $graphics ([Math]::Abs($screen.id.GetHashCode()))

    switch ($screen.focus.type) {
        'interactionActions' {
            Draw-MainMenuActionsInteractionDeck $graphics
            return
        }
        'interactionSettings' {
            Draw-MainMenuSettingsInteractionDeck $graphics
            return
        }
    }

    Draw-Rect $graphics 60 50 1416 732 '#87927c' '#020305' 120 1
    Draw-Text $graphics 'RiskyStars' 0 66 $width 42 34 '#9bdc65' 'Center'
    Draw-Text $graphics 'Fleet command interface' 0 112 $width 32 22 '#d8d8d8' 'Center'
    switch ($screen.focus.type) {
        'actions' {
            Draw-Text $graphics 'SECTOR BRIEF' 88 180 240 30 17 '#d8d8d8'
            Draw-Text $graphics 'Chart a system. Build a faction. Risk the stars.' 88 214 870 44 32 '#9bdc65'
            Draw-Text $graphics 'A framed command deck for multiplayer campaigns, fast single-player setup, and in-game ship-console tooling.' 88 266 930 34 18 '#d8d8d8'
            Draw-Rect $graphics 1180 166 280 476 '#9bdc65' '#05070c' 170 1
            Draw-Text $graphics 'Command Actions' 1196 183 248 34 24 '#d8d8d8'
            $labels = @('MULTIPLAYER', 'SINGLE PLAYER', 'TUTORIAL MODE', 'SETTINGS', 'EXIT')
            for ($i = 0; $i -lt $labels.Count; $i++) {
                $fill = if ($labels[$i] -eq 'EXIT') { '#d66050' } elseif ($labels[$i] -eq 'TUTORIAL MODE') { '#d8ce9f' } else { '#15191f' }
                Draw-Button $graphics $labels[$i] 1204 (220 + ($i * 66)) 236 56 $fill
            }
        }
        'centerPanel' {
            Draw-Rect $graphics 460 170 620 430 '#9bdc65' '#05070c' 190 1
            Draw-Text $graphics $screen.focus.label 488 198 560 42 26 '#9bdc65'
            Draw-Rect $graphics 500 260 520 210 '#76806f' '#0b0f14' 190 1
            Draw-Text $graphics $screen.prompt 525 286 470 144 18 '#d8d8d8'
            Draw-Button $graphics 'PRIMARY ACTION' 540 510 190 48 '#243b1f'
            Draw-Button $graphics 'BACK' 760 510 150 48 '#15191f'
        }
        default {
            Draw-Rect $graphics 220 150 1080 560 '#9bdc65' '#05070c' 185 1
            Draw-Text $graphics $screen.focus.label 252 175 980 38 26 '#9bdc65'
            Draw-Rect $graphics 252 245 980 300 '#76806f' '#0b0f14' 185 1
            Draw-Text $graphics $screen.prompt 285 270 900 170 18 '#d8d8d8'
            Draw-Button $graphics 'CREATE / CONTINUE' 900 585 210 48 '#243b1f'
            Draw-Button $graphics 'BACK / CANCEL' 1130 585 170 48 '#15191f'
        }
    }
}

function Draw-MainMenuActionsInteractionDeck($graphics) {
    Draw-InteractionStarMapBackdrop $graphics
    Draw-Rect $graphics 60 50 1416 732 '#87927c' '#020305' 105 1
    Draw-Text $graphics 'RiskyStars' 0 66 $width 42 34 '#9bdc65' 'Center'
    Draw-Text $graphics 'Fleet command interface' 0 112 $width 32 22 '#d8d8d8' 'Center'
    Draw-Text $graphics 'SECTOR BRIEF' 88 180 260 28 18 '#d8d8d8'
    Draw-Text $graphics 'Chart a system. Build a faction. Risk the stars.' 88 210 900 46 32 '#9bdc65'
    Draw-Text $graphics 'A framed command deck for multiplayer campaigns, fast single-player setup, and in-game ship-console tooling.' 88 264 1080 30 16 '#d8d8d8'
    Draw-MenuBullet $graphics 'Multiplayer lobbies with a shared command shell.' 88 302 650
    Draw-MenuBullet $graphics 'Single-player lineup builder with AI command slots.' 88 334 650
    Draw-MenuBullet $graphics 'Dockable in-game windows styled as one console family.' 88 366 760

    Draw-Rect $graphics 1180 166 280 476 '#87927c' '#05070c' 170 1
    Draw-Text $graphics 'Command Actions' 1196 183 248 34 24 '#d8d8d8'
    $labels = @('MULTIPLAYER', 'SINGLE PLAYER', 'TUTORIAL MODE', 'SETTINGS', 'EXIT')
    for ($i = 0; $i -lt $labels.Count; $i++) {
        $fill = if ($labels[$i] -eq 'EXIT') { '#d66050' } elseif ($labels[$i] -eq 'TUTORIAL MODE') { '#d8ce9f' } else { '#15191f' }
        Draw-Button $graphics $labels[$i] 1204 (220 + ($i * 66)) 236 56 $fill
    }

    Draw-Text $graphics 'Default uplink: http://localhost:5000' 76 674 520 30 16 '#d8d8d8'
    Draw-Text $graphics 'Build 1.0.0-0' 1250 674 180 30 16 '#d8d8d8'
}

function Draw-MainMenuSettingsInteractionDeck($graphics) {
    Draw-InteractionStarMapBackdrop $graphics
    Draw-Rect $graphics 70 37 1396 758 '#87927c' '#020305' 105 1
    Draw-Text $graphics 'Command Settings' 0 62 $width 42 34 '#9bdc65' 'Center'
    Draw-Text $graphics 'Display, server, session, and theme controls' 0 110 $width 32 22 '#d8d8d8' 'Center'

    Draw-Rect $graphics 86 168 636 364 '#87927c' '#05070c' 160 1
    Draw-Text $graphics 'Server Endpoint' 100 185 540 34 24 '#d8d8d8'
    Draw-Text $graphics 'Used as the default multiplayer uplink.' 100 225 560 28 16 '#d8d8d8'
    Draw-Rect $graphics 100 252 598 50 '#87927c' '#0b0f14' 205 1
    Draw-Text $graphics 'http://localhost:5000' 116 268 520 24 18 '#d8d8d8'

    Draw-Rect $graphics 748 168 636 364 '#87927c' '#05070c' 160 1
    Draw-Text $graphics 'Display Profile' 762 185 540 34 24 '#d8d8d8'
    Draw-Text $graphics 'Choose the monitor profile for the command deck.' 762 225 560 28 16 '#d8d8d8'
    Draw-Text $graphics 'Resolution' 766 268 250 26 18 '#d8d8d8'
    Draw-Text $graphics 'Choose the backbuffer size used in windowed or fullscreen mode.' 766 296 280 44 14 '#d8d8d8'
    Draw-Rect $graphics 766 348 260 40 '#87927c' '#0b0f14' 205 1
    Draw-Text $graphics '1280x720' 782 359 180 22 16 '#d8d8d8'
    Draw-Text $graphics 'Window Mode' 1074 268 250 26 18 '#d8d8d8'
    Draw-Text $graphics 'Switch between a resizable window and fullscreen output.' 1074 296 280 44 14 '#d8d8d8'
    Draw-Rect $graphics 1074 348 260 40 '#87927c' '#0b0f14' 205 1
    Draw-Text $graphics 'Normal' 1090 359 180 22 16 '#d8d8d8'
    Draw-Text $graphics 'UI Scale' 766 418 250 26 18 '#d8d8d8'
    Draw-Text $graphics 'Scales Myra menus, panels, buttons, and window chrome.' 766 446 430 28 14 '#d8d8d8'
    Draw-Rect $graphics 766 486 340 8 '#87927c' '#202830' 230 1
    Use-Brush '#9bdc65' 200 { param($brush) $graphics.FillRectangle($brush, 767, 487, 170, 6) }
    Draw-Text $graphics '100%' 1130 473 92 26 16 '#d8d8d8'

    Draw-Rect $graphics 86 556 1298 112 '#87927c' '#05070c' 160 1
    Draw-Text $graphics 'Visual Palette' 100 574 560 34 24 '#d8d8d8'
    Draw-Text $graphics 'Accent colors live here. Geometry and scale are unified across gameplay and Myra surfaces, so there are no separate spacing or chrome controls.' 100 614 1240 30 14 '#d8d8d8'
    Draw-Text $graphics 'Palette' 100 652 320 34 22 '#d8d8d8'
    Draw-Text $graphics 'Accent Color' 335 650 170 24 16 '#d8d8d8'
    Draw-Text $graphics 'Classic Green' 510 650 170 24 16 '#9bdc65'
    Draw-Text $graphics 'Warning Tone' 745 650 170 24 16 '#d8d8d8'
    Draw-Text $graphics 'Amber' 920 650 120 24 16 '#d8ce9f'

    Draw-Rect $graphics 1434 168 16 500 '#76806f' '#15191f' 220 1
    Use-Brush '#d8d8d8' 150 { param($brush) $graphics.FillRectangle($brush, 1439, 168, 7, 382) }
    Draw-Button $graphics 'SAVE SETTINGS' 556 726 220 54 '#15191f'
    Draw-Button $graphics 'BACK' 800 726 180 54 '#15191f'
}

function Draw-Focus($graphics, $focus) {
    switch ($focus.type) {
        'topBar' { Draw-Rect $graphics 0 0 $width 126 '#ffffff' '#9bdc65' 25 3 }
        'rightPanel' { Draw-Rect $graphics 1340 175 186 470 '#ffffff' '#9bdc65' 25 3 }
        'sideRails' {
            Draw-Rect $graphics 0 126 268 ($height - 126) '#ffffff' '#9bdc65' 20 3
            Draw-Rect $graphics 1300 126 236 ($height - 126) '#ffffff' '#9bdc65' 20 3
        }
        'leftRail' { Draw-Rect $graphics 0 126 268 130 '#ffffff' '#9bdc65' 25 3 }
        'mapObject' { Draw-Rect $graphics 700 338 210 190 '#ffffff' '#9bdc65' 25 3 }
        'contextMenu' {
            Draw-Rect $graphics $focus.x $focus.y $focus.w $focus.h '#ffffff' '#15191f' 230 3
            Draw-Text $graphics $focus.label ($focus.x + 16) ($focus.y + 16) ($focus.w - 30) 30 22 '#d8d8d8'
            Draw-Button $graphics 'View Info' ($focus.x + 20) ($focus.y + 62) ($focus.w - 40) 36 '#15191f'
            Draw-Button $graphics 'Reinforce' ($focus.x + 20) ($focus.y + 106) ($focus.w - 40) 36 '#15191f'
            Draw-Button $graphics 'Diplomacy' ($focus.x + 20) ($focus.y + 150) ($focus.w - 40) 36 '#15191f'
        }
        'planetWindow' {
            Draw-Rect $graphics $focus.x $focus.y $focus.w $focus.h '#9bdc65' '#05070c' 220 2
            Draw-Text $graphics $focus.label ($focus.x + 20) ($focus.y + 18) ($focus.w - 40) 38 26 '#d8d8d8'
            $cx = $focus.x + [int]($focus.w / 2)
            $cy = $focus.y + 330
            Use-Pen '#9bdc65' 245 3 { param($pen) $graphics.DrawEllipse($pen, $cx - 150, $cy - 150, 300, 300) }
            Draw-Line $graphics ($cx - 150) $cy ($cx + 150) $cy '#9bdc65' 220 2
            Draw-Line $graphics $cx ($cy - 150) $cx ($cy + 150) '#9bdc65' 220 2
            Draw-Button $graphics 'Region A' ($cx - 115) ($cy - 12) 96 28 '#15191f'
            Draw-Button $graphics 'Region B' ($cx + 20) ($cy - 12) 96 28 '#15191f'
        }
        default {
            Draw-Rect $graphics $focus.x $focus.y $focus.w $focus.h '#9bdc65' '#05070c' 220 2
            Draw-Text $graphics $focus.label ($focus.x + 20) ($focus.y + 18) ($focus.w - 40) 36 26 '#d8d8d8'
            Draw-Rect $graphics ($focus.x + 24) ($focus.y + 68) ($focus.w - 48) ($focus.h - 130) '#76806f' '#0b0f14' 190 1
            Draw-Text $graphics 'bounded scroll content' ($focus.x + 44) ($focus.y + 92) ($focus.w - 88) 40 18 '#d8d8d8'
            Draw-Button $graphics 'BACK' ($focus.x + 70) ($focus.y + $focus.h - 58) 130 40 '#15191f'
            Draw-Button $graphics 'NEXT / APPLY' ($focus.x + 220) ($focus.y + $focus.h - 58) 170 40 '#243b1f'
        }
    }
}

function Draw-Combat($graphics, $screen) {
    Draw-GameplayShell $graphics $screen
    Use-Brush '#000000' 120 { param($brush) $graphics.FillRectangle($brush, 0, 126, $width, $height - 126) }
    $focus = $screen.focus
    Draw-Rect $graphics $focus.x $focus.y $focus.w $focus.h '#d66050' '#090202' 225 2
    Draw-Text $graphics $focus.label ($focus.x + 20) ($focus.y + 18) ($focus.w - 40) 36 24 '#d66050' 'Center'
    Draw-Text $graphics 'Attackers' ($focus.x + 30) ($focus.y + 88) 180 32 20 '#d66050'
    Draw-Text $graphics 'Defenders' ($focus.x + $focus.w - 210) ($focus.y + 88) 180 32 20 '#9bdc65'
    Draw-Rect $graphics ($focus.x + 25) ($focus.y + 132) ($focus.w - 50) 190 '#76806f' '#05070c' 190 1
    Draw-Text $graphics 'Rolls | Pairings | Casualties | Survivors' ($focus.x + 45) ($focus.y + 155) ($focus.w - 90) 34 18 '#d8d8d8'
}

foreach ($screen in $catalog.screens) {
    $bitmap = New-Object System.Drawing.Bitmap $width, $height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
            $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

            switch ($screen.mode) {
                'commandDeck' { Draw-CommandDeck $graphics $screen }
                'gameplay' { Draw-GameplayShell $graphics $screen; Draw-Focus $graphics $screen.focus }
                'gameplayWindow' { Draw-GameplayShell $graphics $screen; Draw-Focus $graphics $screen.focus }
                'modal' {
                    Draw-GameplayShell $graphics $screen
                    Use-Brush '#000000' 130 { param($brush) $graphics.FillRectangle($brush, 0, 126, $width, $height - 126) }
                    Draw-Focus $graphics $screen.focus
                }
                'combat' { Draw-Combat $graphics $screen }
                default { Draw-CommandDeck $graphics $screen }
            }

            if (-not $screen.hidePromptBaseline) {
                Draw-Text $graphics "Prompt baseline: $($screen.title)" 12 ($height - 26) 620 20 14 '#9bdc65'
            }
        }
        finally {
            $graphics.Dispose()
        }

        $outputPath = Join-Path $OutputDirectory "$($screen.id).png"
        $bitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $bitmap.Dispose()
    }
}
