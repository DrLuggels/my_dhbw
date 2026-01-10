// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'exercise_model.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
    'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models');

ExerciseModel _$ExerciseModelFromJson(Map<String, dynamic> json) {
  return _ExerciseModel.fromJson(json);
}

/// @nodoc
mixin _$ExerciseModel {
  @HiveField(0)
  int get id => throw _privateConstructorUsedError;
  @HiveField(1)
  int get userId => throw _privateConstructorUsedError;
  @HiveField(2)
  String get subject => throw _privateConstructorUsedError;
  @HiveField(3)
  String get questionText => throw _privateConstructorUsedError;
  @HiveField(4)
  String? get correctAnswer => throw _privateConstructorUsedError;
  @HiveField(5)
  String? get hint => throw _privateConstructorUsedError;
  @HiveField(6)
  DateTime get nextReviewDate => throw _privateConstructorUsedError;
  @HiveField(7)
  double get easeFactor => throw _privateConstructorUsedError; // SM-2 Algorithm
  @HiveField(8)
  int get reviewCount => throw _privateConstructorUsedError;
  @HiveField(9)
  int get repetitions => throw _privateConstructorUsedError;
  @HiveField(10)
  String get difficulty => throw _privateConstructorUsedError;
  @HiveField(11)
  String? get lastAnswer => throw _privateConstructorUsedError;
  @HiveField(12)
  DateTime? get lastReviewedAt => throw _privateConstructorUsedError;
  @HiveField(13)
  String? get createdAt => throw _privateConstructorUsedError;

  /// Serializes this ExerciseModel to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of ExerciseModel
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $ExerciseModelCopyWith<ExerciseModel> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $ExerciseModelCopyWith<$Res> {
  factory $ExerciseModelCopyWith(
          ExerciseModel value, $Res Function(ExerciseModel) then) =
      _$ExerciseModelCopyWithImpl<$Res, ExerciseModel>;
  @useResult
  $Res call(
      {@HiveField(0) int id,
      @HiveField(1) int userId,
      @HiveField(2) String subject,
      @HiveField(3) String questionText,
      @HiveField(4) String? correctAnswer,
      @HiveField(5) String? hint,
      @HiveField(6) DateTime nextReviewDate,
      @HiveField(7) double easeFactor,
      @HiveField(8) int reviewCount,
      @HiveField(9) int repetitions,
      @HiveField(10) String difficulty,
      @HiveField(11) String? lastAnswer,
      @HiveField(12) DateTime? lastReviewedAt,
      @HiveField(13) String? createdAt});
}

/// @nodoc
class _$ExerciseModelCopyWithImpl<$Res, $Val extends ExerciseModel>
    implements $ExerciseModelCopyWith<$Res> {
  _$ExerciseModelCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of ExerciseModel
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? userId = null,
    Object? subject = null,
    Object? questionText = null,
    Object? correctAnswer = freezed,
    Object? hint = freezed,
    Object? nextReviewDate = null,
    Object? easeFactor = null,
    Object? reviewCount = null,
    Object? repetitions = null,
    Object? difficulty = null,
    Object? lastAnswer = freezed,
    Object? lastReviewedAt = freezed,
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
      subject: null == subject
          ? _value.subject
          : subject // ignore: cast_nullable_to_non_nullable
              as String,
      questionText: null == questionText
          ? _value.questionText
          : questionText // ignore: cast_nullable_to_non_nullable
              as String,
      correctAnswer: freezed == correctAnswer
          ? _value.correctAnswer
          : correctAnswer // ignore: cast_nullable_to_non_nullable
              as String?,
      hint: freezed == hint
          ? _value.hint
          : hint // ignore: cast_nullable_to_non_nullable
              as String?,
      nextReviewDate: null == nextReviewDate
          ? _value.nextReviewDate
          : nextReviewDate // ignore: cast_nullable_to_non_nullable
              as DateTime,
      easeFactor: null == easeFactor
          ? _value.easeFactor
          : easeFactor // ignore: cast_nullable_to_non_nullable
              as double,
      reviewCount: null == reviewCount
          ? _value.reviewCount
          : reviewCount // ignore: cast_nullable_to_non_nullable
              as int,
      repetitions: null == repetitions
          ? _value.repetitions
          : repetitions // ignore: cast_nullable_to_non_nullable
              as int,
      difficulty: null == difficulty
          ? _value.difficulty
          : difficulty // ignore: cast_nullable_to_non_nullable
              as String,
      lastAnswer: freezed == lastAnswer
          ? _value.lastAnswer
          : lastAnswer // ignore: cast_nullable_to_non_nullable
              as String?,
      lastReviewedAt: freezed == lastReviewedAt
          ? _value.lastReviewedAt
          : lastReviewedAt // ignore: cast_nullable_to_non_nullable
              as DateTime?,
      createdAt: freezed == createdAt
          ? _value.createdAt
          : createdAt // ignore: cast_nullable_to_non_nullable
              as String?,
    ) as $Val);
  }
}

/// @nodoc
abstract class _$$ExerciseModelImplCopyWith<$Res>
    implements $ExerciseModelCopyWith<$Res> {
  factory _$$ExerciseModelImplCopyWith(
          _$ExerciseModelImpl value, $Res Function(_$ExerciseModelImpl) then) =
      __$$ExerciseModelImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call(
      {@HiveField(0) int id,
      @HiveField(1) int userId,
      @HiveField(2) String subject,
      @HiveField(3) String questionText,
      @HiveField(4) String? correctAnswer,
      @HiveField(5) String? hint,
      @HiveField(6) DateTime nextReviewDate,
      @HiveField(7) double easeFactor,
      @HiveField(8) int reviewCount,
      @HiveField(9) int repetitions,
      @HiveField(10) String difficulty,
      @HiveField(11) String? lastAnswer,
      @HiveField(12) DateTime? lastReviewedAt,
      @HiveField(13) String? createdAt});
}

/// @nodoc
class __$$ExerciseModelImplCopyWithImpl<$Res>
    extends _$ExerciseModelCopyWithImpl<$Res, _$ExerciseModelImpl>
    implements _$$ExerciseModelImplCopyWith<$Res> {
  __$$ExerciseModelImplCopyWithImpl(
      _$ExerciseModelImpl _value, $Res Function(_$ExerciseModelImpl) _then)
      : super(_value, _then);

  /// Create a copy of ExerciseModel
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? userId = null,
    Object? subject = null,
    Object? questionText = null,
    Object? correctAnswer = freezed,
    Object? hint = freezed,
    Object? nextReviewDate = null,
    Object? easeFactor = null,
    Object? reviewCount = null,
    Object? repetitions = null,
    Object? difficulty = null,
    Object? lastAnswer = freezed,
    Object? lastReviewedAt = freezed,
    Object? createdAt = freezed,
  }) {
    return _then(_$ExerciseModelImpl(
      id: null == id
          ? _value.id
          : id // ignore: cast_nullable_to_non_nullable
              as int,
      userId: null == userId
          ? _value.userId
          : userId // ignore: cast_nullable_to_non_nullable
              as int,
      subject: null == subject
          ? _value.subject
          : subject // ignore: cast_nullable_to_non_nullable
              as String,
      questionText: null == questionText
          ? _value.questionText
          : questionText // ignore: cast_nullable_to_non_nullable
              as String,
      correctAnswer: freezed == correctAnswer
          ? _value.correctAnswer
          : correctAnswer // ignore: cast_nullable_to_non_nullable
              as String?,
      hint: freezed == hint
          ? _value.hint
          : hint // ignore: cast_nullable_to_non_nullable
              as String?,
      nextReviewDate: null == nextReviewDate
          ? _value.nextReviewDate
          : nextReviewDate // ignore: cast_nullable_to_non_nullable
              as DateTime,
      easeFactor: null == easeFactor
          ? _value.easeFactor
          : easeFactor // ignore: cast_nullable_to_non_nullable
              as double,
      reviewCount: null == reviewCount
          ? _value.reviewCount
          : reviewCount // ignore: cast_nullable_to_non_nullable
              as int,
      repetitions: null == repetitions
          ? _value.repetitions
          : repetitions // ignore: cast_nullable_to_non_nullable
              as int,
      difficulty: null == difficulty
          ? _value.difficulty
          : difficulty // ignore: cast_nullable_to_non_nullable
              as String,
      lastAnswer: freezed == lastAnswer
          ? _value.lastAnswer
          : lastAnswer // ignore: cast_nullable_to_non_nullable
              as String?,
      lastReviewedAt: freezed == lastReviewedAt
          ? _value.lastReviewedAt
          : lastReviewedAt // ignore: cast_nullable_to_non_nullable
              as DateTime?,
      createdAt: freezed == createdAt
          ? _value.createdAt
          : createdAt // ignore: cast_nullable_to_non_nullable
              as String?,
    ));
  }
}

/// @nodoc
@JsonSerializable()
class _$ExerciseModelImpl implements _ExerciseModel {
  const _$ExerciseModelImpl(
      {@HiveField(0) required this.id,
      @HiveField(1) required this.userId,
      @HiveField(2) required this.subject,
      @HiveField(3) required this.questionText,
      @HiveField(4) this.correctAnswer,
      @HiveField(5) this.hint,
      @HiveField(6) required this.nextReviewDate,
      @HiveField(7) this.easeFactor = 2.5,
      @HiveField(8) this.reviewCount = 0,
      @HiveField(9) this.repetitions = 0,
      @HiveField(10) this.difficulty = 'medium',
      @HiveField(11) this.lastAnswer,
      @HiveField(12) this.lastReviewedAt,
      @HiveField(13) this.createdAt});

  factory _$ExerciseModelImpl.fromJson(Map<String, dynamic> json) =>
      _$$ExerciseModelImplFromJson(json);

  @override
  @HiveField(0)
  final int id;
  @override
  @HiveField(1)
  final int userId;
  @override
  @HiveField(2)
  final String subject;
  @override
  @HiveField(3)
  final String questionText;
  @override
  @HiveField(4)
  final String? correctAnswer;
  @override
  @HiveField(5)
  final String? hint;
  @override
  @HiveField(6)
  final DateTime nextReviewDate;
  @override
  @JsonKey()
  @HiveField(7)
  final double easeFactor;
// SM-2 Algorithm
  @override
  @JsonKey()
  @HiveField(8)
  final int reviewCount;
  @override
  @JsonKey()
  @HiveField(9)
  final int repetitions;
  @override
  @JsonKey()
  @HiveField(10)
  final String difficulty;
  @override
  @HiveField(11)
  final String? lastAnswer;
  @override
  @HiveField(12)
  final DateTime? lastReviewedAt;
  @override
  @HiveField(13)
  final String? createdAt;

  @override
  String toString() {
    return 'ExerciseModel(id: $id, userId: $userId, subject: $subject, questionText: $questionText, correctAnswer: $correctAnswer, hint: $hint, nextReviewDate: $nextReviewDate, easeFactor: $easeFactor, reviewCount: $reviewCount, repetitions: $repetitions, difficulty: $difficulty, lastAnswer: $lastAnswer, lastReviewedAt: $lastReviewedAt, createdAt: $createdAt)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ExerciseModelImpl &&
            (identical(other.id, id) || other.id == id) &&
            (identical(other.userId, userId) || other.userId == userId) &&
            (identical(other.subject, subject) || other.subject == subject) &&
            (identical(other.questionText, questionText) ||
                other.questionText == questionText) &&
            (identical(other.correctAnswer, correctAnswer) ||
                other.correctAnswer == correctAnswer) &&
            (identical(other.hint, hint) || other.hint == hint) &&
            (identical(other.nextReviewDate, nextReviewDate) ||
                other.nextReviewDate == nextReviewDate) &&
            (identical(other.easeFactor, easeFactor) ||
                other.easeFactor == easeFactor) &&
            (identical(other.reviewCount, reviewCount) ||
                other.reviewCount == reviewCount) &&
            (identical(other.repetitions, repetitions) ||
                other.repetitions == repetitions) &&
            (identical(other.difficulty, difficulty) ||
                other.difficulty == difficulty) &&
            (identical(other.lastAnswer, lastAnswer) ||
                other.lastAnswer == lastAnswer) &&
            (identical(other.lastReviewedAt, lastReviewedAt) ||
                other.lastReviewedAt == lastReviewedAt) &&
            (identical(other.createdAt, createdAt) ||
                other.createdAt == createdAt));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
      runtimeType,
      id,
      userId,
      subject,
      questionText,
      correctAnswer,
      hint,
      nextReviewDate,
      easeFactor,
      reviewCount,
      repetitions,
      difficulty,
      lastAnswer,
      lastReviewedAt,
      createdAt);

  /// Create a copy of ExerciseModel
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ExerciseModelImplCopyWith<_$ExerciseModelImpl> get copyWith =>
      __$$ExerciseModelImplCopyWithImpl<_$ExerciseModelImpl>(this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$ExerciseModelImplToJson(
      this,
    );
  }
}

abstract class _ExerciseModel implements ExerciseModel {
  const factory _ExerciseModel(
      {@HiveField(0) required final int id,
      @HiveField(1) required final int userId,
      @HiveField(2) required final String subject,
      @HiveField(3) required final String questionText,
      @HiveField(4) final String? correctAnswer,
      @HiveField(5) final String? hint,
      @HiveField(6) required final DateTime nextReviewDate,
      @HiveField(7) final double easeFactor,
      @HiveField(8) final int reviewCount,
      @HiveField(9) final int repetitions,
      @HiveField(10) final String difficulty,
      @HiveField(11) final String? lastAnswer,
      @HiveField(12) final DateTime? lastReviewedAt,
      @HiveField(13) final String? createdAt}) = _$ExerciseModelImpl;

  factory _ExerciseModel.fromJson(Map<String, dynamic> json) =
      _$ExerciseModelImpl.fromJson;

  @override
  @HiveField(0)
  int get id;
  @override
  @HiveField(1)
  int get userId;
  @override
  @HiveField(2)
  String get subject;
  @override
  @HiveField(3)
  String get questionText;
  @override
  @HiveField(4)
  String? get correctAnswer;
  @override
  @HiveField(5)
  String? get hint;
  @override
  @HiveField(6)
  DateTime get nextReviewDate;
  @override
  @HiveField(7)
  double get easeFactor; // SM-2 Algorithm
  @override
  @HiveField(8)
  int get reviewCount;
  @override
  @HiveField(9)
  int get repetitions;
  @override
  @HiveField(10)
  String get difficulty;
  @override
  @HiveField(11)
  String? get lastAnswer;
  @override
  @HiveField(12)
  DateTime? get lastReviewedAt;
  @override
  @HiveField(13)
  String? get createdAt;

  /// Create a copy of ExerciseModel
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ExerciseModelImplCopyWith<_$ExerciseModelImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
