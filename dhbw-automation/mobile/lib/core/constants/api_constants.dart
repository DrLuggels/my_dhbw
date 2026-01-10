class ApiConstants {
  // Base URL - Server Backend
  static const String baseUrl = 'http://192.168.178.198:5000/api';

  // Auth Endpoints
  static const String login = '/auth/login';
  static const String register = '/auth/register';
  static const String changePassword = '/auth/change-password';

  // Files Endpoints
  static const String uploadFile = '/files/upload';
  static const String getFiles = '/files';
  static const String deleteFile = '/files'; // + /{id}
  static const String bulkDeleteFiles = '/files/bulk-delete';
  static const String downloadFile = '/files/download'; // + /{id}

  // Calendar Endpoints
  static const String getEvents = '/calendar/events'; // + /{userId}
  static const String weekSchedule = '/calendar/week-schedule';
  static const String createEvent = '/calendar/events';
  static const String deleteEvent = '/calendar/events'; // + /{eventId}
  static const String updateEventNotes = '/calendar'; // + /{eventId}/notes
  static const String syncRapla = '/calendar/sync-rapla'; // + /{userId}

  // Validation Endpoints
  static const String pendingEntities = '/validation/pending';
  static const String answerQuestions = '/validation'; // + /{id}/answer
  static const String confirmEntity = '/validation'; // + /{id}/confirm
  static const String rejectEntity = '/validation'; // + /{id}/reject
  static const String modifyEntity = '/validation'; // + /{id}
  static const String bulkConfirm = '/validation/bulk-confirm';

  // Learning Endpoints
  static const String learningDeficits = '/learning/deficits'; // + /{userId}
  static const String dueExercises = '/learning/exercises/due'; // + /{userId}
  static const String userExercises = '/learning/exercises/user'; // + /{userId}
  static const String submitAnswer = '/learning/exercises'; // + /{exerciseId}/answer
  static const String scheduleTutoring = '/learning/schedule-tutoring'; // + /{deficitId}
  static const String resolveDeficit = '/learning/deficits'; // + /{deficitId}/resolve
  static const String learningStats = '/learning/stats'; // + /{userId}

  // Request timeouts
  static const int connectTimeout = 30000; // 30 seconds
  static const int receiveTimeout = 30000; // 30 seconds

  // File upload limits
  static const int maxFileSize = 100 * 1024 * 1024; // 100 MB
}
