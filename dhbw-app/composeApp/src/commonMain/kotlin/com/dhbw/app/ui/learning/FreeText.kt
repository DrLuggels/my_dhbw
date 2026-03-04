package com.dhbw.app.ui.learning

import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.heightIn
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp

@Composable
fun FreeText(
    value: String,
    onValueChange: (String) -> Unit,
) {
    OutlinedTextField(
        value = value,
        onValueChange = onValueChange,
        label = { Text("Deine Antwort") },
        placeholder = { Text("Ausführliche Antwort eingeben...") },
        minLines = 4,
        maxLines = 8,
        modifier = Modifier.fillMaxWidth().heightIn(min = 120.dp),
    )
}
