class ApiConfig {
  static const String baseUrl = 'http://localhost:5000/api';
  static const String wsUrl = 'ws://localhost:5000';
  
  // Endpoints
  static const String auth = '/auth';
  static const String calendar = '/calendar';
  static const String todos = '/todo';
  static const String files = '/files';
  static const String mail = '/mail';
  static const String learning = '/learning';
  static const String interaction = '/interaction';
  
  // Polling intervals
  static const Duration pollInterval = Duration(seconds: 30);
  static const Duration backgroundSyncInterval = Duration(minutes: 15);
}
