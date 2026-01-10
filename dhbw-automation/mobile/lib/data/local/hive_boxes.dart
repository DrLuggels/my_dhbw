import 'package:hive_flutter/hive_flutter.dart';
import '../models/files/document_model.dart';
import '../models/calendar/calendar_event_model.dart';

/// Hive Database Manager
/// Handles initialization and box management for offline storage
class HiveBoxes {
  // Box names
  static const String documentsBox = 'documents';
  static const String eventsBox = 'events';
  static const String exercisesBox = 'exercises';

  // Singleton instance
  static final HiveBoxes _instance = HiveBoxes._internal();
  factory HiveBoxes() => _instance;
  HiveBoxes._internal();

  bool _initialized = false;

  /// Initialize Hive with Flutter and register adapters
  Future<void> init() async {
    if (_initialized) return;

    // Initialize Hive for Flutter
    await Hive.initFlutter();

    // Register TypeAdapters
    Hive.registerAdapter(DocumentModelAdapter());
    Hive.registerAdapter(CalendarEventModelAdapter());
    // TODO: Register when models are ready:
    // Hive.registerAdapter(ExerciseModelAdapter());

    // Open boxes (typed)
    await Hive.openBox<DocumentModel>(documentsBox);
    await Hive.openBox<CalendarEventModel>(eventsBox);
    await Hive.openBox(exercisesBox);

    _initialized = true;
  }

  /// Get Documents Box
  Box<DocumentModel> getDocumentsBox() {
    return Hive.box<DocumentModel>(documentsBox);
  }

  /// Get Events Box
  Box<CalendarEventModel> getEventsBox() {
    return Hive.box<CalendarEventModel>(eventsBox);
  }

  /// Get Exercises Box
  Box getExercisesBox() {
    return Hive.box(exercisesBox);
  }

  /// Close all boxes
  Future<void> closeAll() async {
    await Hive.close();
    _initialized = false;
  }

  /// Clear all data from all boxes
  Future<void> clearAll() async {
    await getDocumentsBox().clear();
    await getEventsBox().clear();
    await getExercisesBox().clear();
  }
}
