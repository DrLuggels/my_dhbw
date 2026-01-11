import 'package:freezed_annotation/freezed_annotation.dart';
import 'package:hive/hive.dart';

part 'calendar_event_model.freezed.dart';
part 'calendar_event_model.g.dart';

/// Calendar Event Model with Hive support for offline caching
@freezed
@HiveType(typeId: 2)
class CalendarEventModel with _$CalendarEventModel {
  const factory CalendarEventModel({
    @HiveField(0) required int id,
    @HiveField(1) @Default(1) int userId,
    @HiveField(2) required String title,
    @HiveField(3) required DateTime startTime,
    @HiveField(4) required DateTime endTime,
    @HiveField(5) @Default('') String location,
    @HiveField(6) @Default('') String subject,
    @HiveField(7) @Default('manual') String source, // 'rapla', 'moodle', 'manual'
    @HiveField(8) String? eventType,
    @HiveField(9) String? description,
    @HiveField(10) String? professor,
    @HiveField(11) String? notes,
    @HiveField(12) String? createdAt,
  }) = _CalendarEventModel;

  factory CalendarEventModel.fromJson(Map<String, dynamic> json) =>
      _$CalendarEventModelFromJson(json);
}
