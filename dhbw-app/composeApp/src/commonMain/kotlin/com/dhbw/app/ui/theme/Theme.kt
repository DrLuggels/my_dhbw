package com.dhbw.app.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable

private val LightColorScheme = lightColorScheme(
    primary = Primary,
    onPrimary = Surface,
    primaryContainer = PrimaryLight,
    secondary = Accent,
    onSecondary = Surface,
    background = Background,
    surface = Surface,
    surfaceVariant = SurfaceVariant,
    error = ErrorRed,
    onBackground = androidx.compose.ui.graphics.Color(0xFF212121),
    onSurface = androidx.compose.ui.graphics.Color(0xFF212121),
)

@Composable
fun DhbwTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = LightColorScheme,
        content = content,
    )
}
