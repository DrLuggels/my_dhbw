package com.dhbw.app.ui.learning

import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier

@Composable
fun FillInBlank(
    value: String,
    onValueChange: (String) -> Unit,
) {
    OutlinedTextField(
        value = value,
        onValueChange = onValueChange,
        label = { Text("Deine Antwort") },
        placeholder = { Text("Antwort eingeben...") },
        singleLine = true,
        modifier = Modifier.fillMaxWidth(),
    )
}
