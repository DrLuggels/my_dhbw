package com.dhbw.app.ui.camera

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import cafe.adriel.voyager.koin.koinScreenModel
import com.dhbw.app.ui.theme.Accent
import com.dhbw.app.ui.theme.ErrorRed
import com.dhbw.app.ui.theme.Primary
import com.dhbw.app.ui.theme.SuccessGreen

@Composable
fun CameraScreen() {
    val viewModel = koinScreenModel<CameraViewModel>()
    val state by viewModel.state.collectAsState()

    Column(
        modifier = Modifier.fillMaxSize().padding(16.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center,
    ) {
        when (state.phase) {
            CameraPhase.IDLE -> IdleState(viewModel)
            CameraPhase.PREVIEW -> PreviewState(viewModel)
            CameraPhase.UPLOADING -> UploadingState()
            CameraPhase.DONE -> DoneState(state, viewModel)
            CameraPhase.ERROR -> ErrorState(state, viewModel)
        }
    }
}

@Composable
private fun IdleState(viewModel: CameraViewModel) {
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        Text(
            text = "Foto aufnehmen",
            style = MaterialTheme.typography.headlineMedium,
            fontWeight = FontWeight.Bold,
        )
        Text(
            text = "Fotografiere Vorlesungsfolien oder Notizen\nfür automatische OCR-Verarbeitung",
            textAlign = TextAlign.Center,
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
        Spacer(modifier = Modifier.height(24.dp))
        // Camera capture will be injected via expect/actual on Android
        Button(
            onClick = {
                // On Android, this triggers CameraX via ActivityResult
                // For now, placeholder — actual capture in androidMain
            },
            modifier = Modifier.size(width = 200.dp, height = 56.dp),
            colors = ButtonDefaults.buttonColors(containerColor = Primary),
        ) {
            Text("Foto aufnehmen", fontSize = 16.sp)
        }
    }
}

@Composable
private fun PreviewState(viewModel: CameraViewModel) {
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        Text("Foto aufgenommen", style = MaterialTheme.typography.titleLarge)
        // Image preview would go here using actual platform image decoder
        Box(
            modifier = Modifier.size(240.dp),
            contentAlignment = Alignment.Center,
        ) {
            Text("Vorschau", color = MaterialTheme.colorScheme.onSurfaceVariant)
        }
        Row(horizontalArrangement = Arrangement.spacedBy(16.dp)) {
            OutlinedButton(onClick = viewModel::retakePhoto) {
                Text("Nochmal")
            }
            Button(
                onClick = viewModel::uploadPhoto,
                colors = ButtonDefaults.buttonColors(containerColor = Accent),
            ) {
                Text("Hochladen")
            }
        }
    }
}

@Composable
private fun UploadingState() {
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        CircularProgressIndicator()
        Text("Wird verarbeitet...", style = MaterialTheme.typography.bodyLarge)
    }
}

@Composable
private fun DoneState(state: CameraState, viewModel: CameraViewModel) {
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        Text("Erfolgreich!", color = SuccessGreen, fontSize = 24.sp, fontWeight = FontWeight.Bold)
        state.documentTitle?.let {
            Text("Dokument: $it", style = MaterialTheme.typography.bodyLarge)
        }
        Button(onClick = viewModel::reset) {
            Text("Weiteres Foto")
        }
    }
}

@Composable
private fun ErrorState(state: CameraState, viewModel: CameraViewModel) {
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(16.dp),
    ) {
        Text("Fehler", color = ErrorRed, fontSize = 24.sp, fontWeight = FontWeight.Bold)
        Text(state.errorMessage ?: "Unbekannter Fehler")
        Row(horizontalArrangement = Arrangement.spacedBy(16.dp)) {
            OutlinedButton(onClick = viewModel::reset) {
                Text("Abbrechen")
            }
            Button(
                onClick = viewModel::uploadPhoto,
                colors = ButtonDefaults.buttonColors(containerColor = Primary),
            ) {
                Text("Erneut versuchen")
            }
        }
    }
}
