import 'package:hive_flutter/hive_flutter.dart';

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
    // Note: Adapters will be generated when models are created
    // Uncomment when models are ready:
    // Hive.registerAdapter(DocumentModelAdapter());
    // Hive.registerAdapter(CalendarEventModelAdapter());
    // Hive.registerAdapter(ExerciseModelAdapter());

    // Open boxes
    // Note: For now, open as dynamic boxes
    // When models are ready, change to typed boxes: Box<DocumentModel>
    await Hive.openBox(documentsBox);
    await Hive.openBox(eventsBox);
    await Hive.openBox(exercisesBox);

    _initialized = true;
  }

  /// Get Documents Box
  Box getDocumentsBox() {
    return Hive.box(documentsBox);
  }

  /// Get Events Box
  Box getEventsBox() {
    return Hive.box(eventsBox);
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
