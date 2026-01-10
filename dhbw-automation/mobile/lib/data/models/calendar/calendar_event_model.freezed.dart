// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'calendar_event_model.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
    'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models');

CalendarEventModel _$CalendarEventModelFromJson(Map<String, dynamic> json) {
  return _CalendarEventModel.fromJson(json);
}

/// @nodoc
mixin _$CalendarEventModel {
  @HiveField(0)
  int get id => throw _privateConstructorUsedError;
  @HiveField(1)
  int get userId => throw _privateConstructorUsedError;
  @HiveField(2)
  String get title => throw _privateConstructorUsedError;
  @HiveField(3)
  DateTime get startTime => throw _privateConstructorUsedError;
  @HiveField(4)
  DateTime get endTime => throw _privateConstructorUsedError;
  @HiveField(5)
  String get location => throw _privateConstructorUsedError;
  @HiveField(6)
  String get subject => throw _privateConstructorUsedError;
  @HiveField(7)
  String get source =>
      throw _privateConstructorUsedError; // 'rapla', 'moodle', 'manual'
  @HiveField(8)
  String? get eventType => throw _privateConstructorUsedError;
  @HiveField(9)
  String? get description => throw _privateConstructorUsedError;
  @HiveField(10)
  String? get professor => throw _privateConstructorUsedError;
  @HiveField(11)
  String? get notes => throw _privateConstructorUsedError;
  @HiveField(12)
  String? get createdAt => throw _privateConstructorUsedError;

  /// Serializes this CalendarEventModel to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of CalendarEventModel
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $CalendarEventModelCopyWith<CalendarEventModel> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $CalendarEventModelCopyWith<$Res> {
  factory $CalendarEventModelCopyWith(
          CalendarEventModel value, $Res Function(CalendarEventModel) then) =
      _$CalendarEventModelCopyWithImpl<$Res, CalendarEventModel>;
  @useResult
  $Res call(
      {@HiveField(0) int id,
      @HiveField(1) int userId,
      @HiveField(2) String title,
      @HiveField(3) DateTime startTime,
      @HiveField(4) DateTime endTime,
      @HiveField(5) String location,
      @HiveField(6) String subject,
      @HiveField(7) String source,
      @HiveField(8) String? eventType,
      @HiveField(9) String? description,
      @HiveField(10) String? professor,
      @HiveField(11) String? notes,
      @HiveField(12) String? createdAt});
}

/// @nodoc
class _$CalendarEventModelCopyWithImpl<$Res, $Val extends CalendarEventModel>
    implements $CalendarEventModelCopyWith<$Res> {
  _$CalendarEventModelCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of CalendarEventModel
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? userId = null,
    Object? title = null,
    Object? startTime = null,
    Object? endTime = null,
    Object? location = null,
    Object? subject = null,
    Object? source = null,
    Object? eventType = freezed,
    Object? description = freezed,
    Object? professor = freezed,
    Object? notes = freezed,
    Object? createdAt = freezed,
  }) {
    return _then(_value.copyWith(
      id: null == id
          ? _value.id
          : id // ignore: cast_nullable_to_non_nullable
              as int,
      userId: null == userId
          ? _value.userId
          : userId // ignore: cast_nullable_to_non_nullable
              as int,
      title: null == title
          ? _value.title
          : title // ignore: cast_nullable_to_non_nullable
              as String,
      startTime: null == startTime
          ? _value.startTime
          : startTime // ignore: cast_nullable_to_non_nullable
              as DateTime,
      endTime: null == endTime
          ? _value.endTime
          : endTime // ignore: cast_nullable_to_non_nullable
              as DateTime,
      location: null == location
          ? _value.location
          : location // ignore: cast_nullable_to_non_nullable
              as String,
      subject: null == subject
          ? _value.subject
          : subject // ignore: cast_nullable_to_non_nullable
              as String,
      source: null == source
          ? _value.source
          : source // ignore: cast_nullable_to_non_nullable
              as String,
      eventType: freezed == eventType
          ? _value.eventType
          : eventType // ignore: cast_nullable_to_non_nullable
              as String?,
      description: freezed == description
          ? _value.description
          : description // ignore: cast_nullable_to_non_nullable
              as String?,
      professor: freezed == professor
          ? _value.professor
          : professor // ignore: cast_nullable_to_non_nullable
              as String?,
      notes: freezed == notes
          ? _value.notes
          : notes // ignore: cast_nullable_to_non_nullable
              as String?,
      createdAt: freezed == createdAt
          ? _value.createdAt
          : createdAt // ignore: cast_nullable_to_non_nullable
              as String?,
    ) as $Val);
  }
}

/// @nodoc
abstract class _$$CalendarEventModelImplCopyWith<$Res>
    implements $CalendarEventModelCopyWith<$Res> {
  factory _$$CalendarEventModelImplCopyWith(_$CalendarEventModelImpl value,
          $Res Function(_$CalendarEventModelImpl) then) =
      __$$CalendarEventModelImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call(
      {@HiveField(0) int id,
      @HiveField(1) int userId,
      @HiveField(2) String title,
      @HiveField(3) DateTime startTime,
      @HiveField(4) DateTime endTime,
      @HiveField(5) String location,
      @HiveField(6) String subject,
      @HiveField(7) String source,
      @HiveField(8) String? eventType,
      @HiveField(9) String? description,
      @HiveField(10) String? professor,
      @HiveField(11) String? notes,
      @HiveField(12) String? createdAt});
}

/// @nodoc
class __$$CalendarEventModelImplCopyWithImpl<$Res>
    extends _$CalendarEventModelCopyWithImpl<$Res, _$CalendarEventModelImpl>
    implements _$$CalendarEventModelImplCopyWith<$Res> {
  __$$CalendarEventModelImplCopyWithImpl(_$CalendarEventModelImpl _value,
      $Res Function(_$CalendarEventModelImpl) _then)
      : super(_value, _then);

  /// Create a copy of CalendarEventModel
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? userId = null,
    Object? title = null,
    Object? startTime = null,
    Object? endTime = null,
    Object? location = null,
    Object? subject = null,
    Object? source = null,
    Object? eventType = freezed,
    Object? description = freezed,
    Object? professor = freezed,
    Object? notes = freezed,
    Object? createdAt = freezed,
  }) {
    return _then(_$CalendarEventModelImpl(
      id: null == id
          ? _value.id
          : id // ignore: cast_nullable_to_non_nullable
              as int,
      userId: null == userId
          ? _value.userId
          : userId // ignore: cast_nullable_to_non_nullable
              as int,
      title: null == title
          ? _value.title
          : title // ignore: cast_nullable_to_non_nullable
              as String,
      startTime: null == startTime
          ? _value.startTime
          : startTime // ignore: cast_nullable_to_non_nullable
              as DateTime,
      endTime: null == endTime
          ? _value.endTime
          : endTime // ignore: cast_nullable_to_non_nullable
              as DateTime,
      location: null == location
          ? _value.location
          : location // ignore: cast_nullable_to_non_nullable
              as String,
      subject: null == subject
          ? _value.subject
          : subject // ignore: cast_nullable_to_non_nullable
              as String,
      source: null == source
          ? _value.source
          : source // ignore: cast_nullable_to_non_nullable
              as String,
      eventType: freezed == eventType
          ? _value.eventType
          : eventType // ignore: cast_nullable_to_non_nullable
              as String?,
      description: freezed == description
          ? _value.description
          : description // ignore: cast_nullable_to_non_nullable
              as String?,
      professor: freezed == professor
          ? _value.professor
          : professor // ignore: cast_nullable_to_non_nullable
              as String?,
      notes: freezed == notes
          ? _value.notes
          : notes // ignore: cast_nullable_to_non_nullable
              as String?,
      createdAt: freezed == createdAt
          ? _value.createdAt
          : createdAt // ignore: cast_nullable_to_non_nullable
              as String?,
    ));
  }
}

/// @nodoc
@JsonSerializable()
class _$CalendarEventModelImpl implements _CalendarEventModel {
  const _$CalendarEventModelImpl(
      {@HiveField(0) required this.id,
      @HiveField(1) required this.userId,
      @HiveField(2) required this.title,
      @HiveField(3) required this.startTime,
      @HiveField(4) required this.endTime,
      @HiveField(5) required this.location,
      @HiveField(6) required this.subject,
      @HiveField(7) required this.source,
      @HiveField(8) this.eventType,
      @HiveField(9) this.description,
      @HiveField(10) this.professor,
      @HiveField(11) this.notes,
      @HiveField(12) this.createdAt});

  factory _$CalendarEventModelImpl.fromJson(Map<String, dynamic> json) =>
      _$$CalendarEventModelImplFromJson(json);

  @override
  @HiveField(0)
  final int id;
  @override
  @HiveField(1)
  final int userId;
  @override
  @HiveField(2)
  final String title;
  @override
  @HiveField(3)
  final DateTime startTime;
  @override
  @HiveField(4)
  final DateTime endTime;
  @override
  @HiveField(5)
  final String location;
  @override
  @HiveField(6)
  final String subject;
  @override
  @HiveField(7)
  final String source;
// 'rapla', 'moodle', 'manual'
  @override
  @HiveField(8)
  final String? eventType;
  @override
  @HiveField(9)
  final String? description;
  @override
  @HiveField(10)
  final String? professor;
  @override
  @HiveField(11)
  final String? notes;
  @override
  @HiveField(12)
  final String? createdAt;

  @override
  String toString() {
    return 'CalendarEventModel(id: $id, userId: $userId, title: $title, startTime: $startTime, endTime: $endTime, location: $location, subject: $subject, source: $source, eventType: $eventType, description: $description, professor: $professor, notes: $notes, createdAt: $createdAt)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$CalendarEventModelImpl &&
            (identical(other.id, id) || other.id == id) &&
            (identical(other.userId, userId) || other.userId == userId) &&
            (identical(other.title, title) || other.title == title) &&
            (identical(other.startTime, startTime) ||
                other.startTime == startTime) &&
            (identical(other.endTime, endTime) || other.endTime == endTime) &&
            (identical(other.location, location) ||
                other.location == location) &&
            (identical(other.subject, subject) || other.subject == subject) &&
            (identical(other.source, source) || other.source == source) &&
            (identical(other.eventType, eventType) ||
                other.eventType == eventType) &&
            (identical(other.description, description) ||
                other.description == description) &&
            (identical(other.professor, professor) ||
                other.professor == professor) &&
            (identical(other.notes, notes) || other.notes == notes) &&
            (identical(other.createdAt, createdAt) ||
                other.createdAt == createdAt));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
      runtimeType,
      id,
      userId,
      title,
      startTime,
      endTime,
      location,
      subject,
      source,
      eventType,
      description,
      professor,
      notes,
      createdAt);

  /// Create a copy of CalendarEventModel
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$CalendarEventModelImplCopyWith<_$CalendarEventModelImpl> get copyWith =>
      __$$CalendarEventModelImplCopyWithImpl<_$CalendarEventModelImpl>(
          this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$CalendarEventModelImplToJson(
      this,
    );
  }
}

abstract class _CalendarEventModel implements CalendarEventModel {
  const factory _CalendarEventModel(
      {@HiveField(0) required final int id,
      @HiveField(1) required final int userId,
      @HiveField(2) required final String title,
      @HiveField(3) required final DateTime startTime,
      @HiveField(4) required final DateTime endTime,
      @HiveField(5) required final String location,
      @HiveField(6) required final String subject,
      @HiveField(7) required final String source,
      @HiveField(8) final String? eventType,
      @HiveField(9) final String? description,
      @HiveField(10) final String? professor,
      @HiveField(11) final String? notes,
      @HiveField(12) final String? createdAt}) = _$CalendarEventModelImpl;

  factory _CalendarEventModel.fromJson(Map<String, dynamic> json) =
      _$CalendarEventModelImpl.fromJson;

  @override
  @HiveField(0)
  int get id;
  @override
  @HiveField(1)
  int get userId;
  @override
  @HiveField(2)
  String get title;
  @override
  @HiveField(3)
  DateTime get startTime;
  @override
  @HiveField(4)
  DateTime get endTime;
  @override
  @HiveField(5)
  String get location;
  @override
  @HiveField(6)
  String get subject;
  @override
  @HiveField(7)
  String get source; // 'rapla', 'moodle', 'manual'
  @override
  @HiveField(8)
  String? get eventType;
  @override
  @HiveField(9)
  String? get description;
  @override
  @HiveField(10)
  String? get professor;
  @override
  @HiveField(11)
  String? get notes;
  @override
  @HiveField(12)
  String? get createdAt;

  /// Create a copy of CalendarEventModel
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$CalendarEventModelImplCopyWith<_$CalendarEventModelImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
