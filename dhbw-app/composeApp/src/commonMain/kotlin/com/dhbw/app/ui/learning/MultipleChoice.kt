package com.dhbw.app.ui.learning

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.RadioButton
import androidx.compose.material3.RadioButtonDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.dhbw.app.ui.theme.Primary

@Composable
fun MultipleChoice(
    options: List<String>,
    selected: String,
    onSelect: (String) -> Unit,
) {
    Column {
        options.forEach { option ->
            val isSelected = option == selected
            Card(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(vertical = 4.dp)
                    .clickable { onSelect(option) },
                elevation = CardDefaults.cardElevation(if (isSelected) 2.dp else 0.dp),
                border = if (isSelected) BorderStroke(2.dp, Primary) else null,
                colors = CardDefaults.cardColors(
                    containerColor = if (isSelected)
                        Primary.copy(alpha = 0.08f)
                    else
                        MaterialTheme.colorScheme.surface,
                ),
            ) {
                Row(
                    modifier = Modifier.padding(12.dp),
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    RadioButton(
                        selected = isSelected,
                        onClick = { onSelect(option) },
                        colors = RadioButtonDefaults.colors(selectedColor = Primary),
                    )
                    Text(
                        text = option,
                        modifier = Modifier.padding(start = 8.dp),
                        style = MaterialTheme.typography.bodyMedium,
                    )
                }
            }
        }
    }
}
