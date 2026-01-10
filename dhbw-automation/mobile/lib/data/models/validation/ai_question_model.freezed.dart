// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'ai_question_model.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
    'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models');

AIQuestionModel _$AIQuestionModelFromJson(Map<String, dynamic> json) {
  return _AIQuestionModel.fromJson(json);
}

/// @nodoc
mixin _$AIQuestionModel {
  int get id => throw _privateConstructorUsedError;
  int get stagedEntityId => throw _privateConstructorUsedError;
  String get fieldName => throw _privateConstructorUsedError;
  String get questionText => throw _privateConstructorUsedError;
  String get answerType =>
      throw _privateConstructorUsedError; // 'text', 'date', 'time', 'datetime', 'choice', 'number'
  String get priority =>
      throw _privateConstructorUsedError; // 'critical', 'high', 'medium', 'low'
  bool get isAnswered => throw _privateConstructorUsedError;
  String? get userAnswer => throw _privateConstructorUsedError;
  String? get suggestedAnswers =>
      throw _privateConstructorUsedError; // JSON array string
  String? get answeredAt => throw _privateConstructorUsedError;

  /// Serializes this AIQuestionModel to a JSON map.
  Map<String, dynamic> toJson() => throw _privateConstructorUsedError;

  /// Create a copy of AIQuestionModel
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $AIQuestionModelCopyWith<AIQuestionModel> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $AIQuestionModelCopyWith<$Res> {
  factory $AIQuestionModelCopyWith(
          AIQuestionModel value, $Res Function(AIQuestionModel) then) =
      _$AIQuestionModelCopyWithImpl<$Res, AIQuestionModel>;
  @useResult
  $Res call(
      {int id,
      int stagedEntityId,
      String fieldName,
      String questionText,
      String answerType,
      String priority,
      bool isAnswered,
      String? userAnswer,
      String? suggestedAnswers,
      String? answeredAt});
}

/// @nodoc
class _$AIQuestionModelCopyWithImpl<$Res, $Val extends AIQuestionModel>
    implements $AIQuestionModelCopyWith<$Res> {
  _$AIQuestionModelCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of AIQuestionModel
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? stagedEntityId = null,
    Object? fieldName = null,
    Object? questionText = null,
    Object? answerType = null,
    Object? priority = null,
    Object? isAnswered = null,
    Object? userAnswer = freezed,
    Object? suggestedAnswers = freezed,
    Object? answeredAt = freezed,
  }) {
    return _then(_value.copyWith(
      id: null == id
          ? _value.id
          : id // ignore: cast_nullable_to_non_nullable
              as int,
      stagedEntityId: null == stagedEntityId
          ? _value.stagedEntityId
          : stagedEntityId // ignore: cast_nullable_to_non_nullable
              as int,
      fieldName: null == fieldName
          ? _value.fieldName
          : fieldName // ignore: cast_nullable_to_non_nullable
              as String,
      questionText: null == questionText
          ? _value.questionText
          : questionText // ignore: cast_nullable_to_non_nullable
              as String,
      answerType: null == answerType
          ? _value.answerType
          : answerType // ignore: cast_nullable_to_non_nullable
              as String,
      priority: null == priority
          ? _value.priority
          : priority // ignore: cast_nullable_to_non_nullable
              as String,
      isAnswered: null == isAnswered
          ? _value.isAnswered
          : isAnswered // ignore: cast_nullable_to_non_nullable
              as bool,
      userAnswer: freezed == userAnswer
          ? _value.userAnswer
          : userAnswer // ignore: cast_nullable_to_non_nullable
              as String?,
      suggestedAnswers: freezed == suggestedAnswers
          ? _value.suggestedAnswers
          : suggestedAnswers // ignore: cast_nullable_to_non_nullable
              as String?,
      answeredAt: freezed == answeredAt
          ? _value.answeredAt
          : answeredAt // ignore: cast_nullable_to_non_nullable
              as String?,
    ) as $Val);
  }
}

/// @nodoc
abstract class _$$AIQuestionModelImplCopyWith<$Res>
    implements $AIQuestionModelCopyWith<$Res> {
  factory _$$AIQuestionModelImplCopyWith(_$AIQuestionModelImpl value,
          $Res Function(_$AIQuestionModelImpl) then) =
      __$$AIQuestionModelImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call(
      {int id,
      int stagedEntityId,
      String fieldName,
      String questionText,
      String answerType,
      String priority,
      bool isAnswered,
      String? userAnswer,
      String? suggestedAnswers,
      String? answeredAt});
}

/// @nodoc
class __$$AIQuestionModelImplCopyWithImpl<$Res>
    extends _$AIQuestionModelCopyWithImpl<$Res, _$AIQuestionModelImpl>
    implements _$$AIQuestionModelImplCopyWith<$Res> {
  __$$AIQuestionModelImplCopyWithImpl(
      _$AIQuestionModelImpl _value, $Res Function(_$AIQuestionModelImpl) _then)
      : super(_value, _then);

  /// Create a copy of AIQuestionModel
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? id = null,
    Object? stagedEntityId = null,
    Object? fieldName = null,
    Object? questionText = null,
    Object? answerType = null,
    Object? priority = null,
    Object? isAnswered = null,
    Object? userAnswer = freezed,
    Object? suggestedAnswers = freezed,
    Object? answeredAt = freezed,
  }) {
    return _then(_$AIQuestionModelImpl(
      id: null == id
          ? _value.id
          : id // ignore: cast_nullable_to_non_nullable
              as int,
      stagedEntityId: null == stagedEntityId
          ? _value.stagedEntityId
          : stagedEntityId // ignore: cast_nullable_to_non_nullable
              as int,
      fieldName: null == fieldName
          ? _value.fieldName
          : fieldName // ignore: cast_nullable_to_non_nullable
              as String,
      questionText: null == questionText
          ? _value.questionText
          : questionText // ignore: cast_nullable_to_non_nullable
              as String,
      answerType: null == answerType
          ? _value.answerType
          : answerType // ignore: cast_nullable_to_non_nullable
              as String,
      priority: null == priority
          ? _value.priority
          : priority // ignore: cast_nullable_to_non_nullable
              as String,
      isAnswered: null == isAnswered
          ? _value.isAnswered
          : isAnswered // ignore: cast_nullable_to_non_nullable
              as bool,
      userAnswer: freezed == userAnswer
          ? _value.userAnswer
          : userAnswer // ignore: cast_nullable_to_non_nullable
              as String?,
      suggestedAnswers: freezed == suggestedAnswers
          ? _value.suggestedAnswers
          : suggestedAnswers // ignore: cast_nullable_to_non_nullable
              as String?,
      answeredAt: freezed == answeredAt
          ? _value.answeredAt
          : answeredAt // ignore: cast_nullable_to_non_nullable
              as String?,
    ));
  }
}

/// @nodoc
@JsonSerializable()
class _$AIQuestionModelImpl implements _AIQuestionModel {
  const _$AIQuestionModelImpl(
      {required this.id,
      required this.stagedEntityId,
      required this.fieldName,
      required this.questionText,
      required this.answerType,
      required this.priority,
      required this.isAnswered,
      this.userAnswer,
      this.suggestedAnswers,
      this.answeredAt});

  factory _$AIQuestionModelImpl.fromJson(Map<String, dynamic> json) =>
      _$$AIQuestionModelImplFromJson(json);

  @override
  final int id;
  @override
  final int stagedEntityId;
  @override
  final String fieldName;
  @override
  final String questionText;
  @override
  final String answerType;
// 'text', 'date', 'time', 'datetime', 'choice', 'number'
  @override
  final String priority;
// 'critical', 'high', 'medium', 'low'
  @override
  final bool isAnswered;
  @override
  final String? userAnswer;
  @override
  final String? suggestedAnswers;
// JSON array string
  @override
  final String? answeredAt;

  @override
  String toString() {
    return 'AIQuestionModel(id: $id, stagedEntityId: $stagedEntityId, fieldName: $fieldName, questionText: $questionText, answerType: $answerType, priority: $priority, isAnswered: $isAnswered, userAnswer: $userAnswer, suggestedAnswers: $suggestedAnswers, answeredAt: $answeredAt)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$AIQuestionModelImpl &&
            (identical(other.id, id) || other.id == id) &&
            (identical(other.stagedEntityId, stagedEntityId) ||
                other.stagedEntityId == stagedEntityId) &&
            (identical(other.fieldName, fieldName) ||
                other.fieldName == fieldName) &&
            (identical(other.questionText, questionText) ||
                other.questionText == questionText) &&
            (identical(other.answerType, answerType) ||
                other.answerType == answerType) &&
            (identical(other.priority, priority) ||
                other.priority == priority) &&
            (identical(other.isAnswered, isAnswered) ||
                other.isAnswered == isAnswered) &&
            (identical(other.userAnswer, userAnswer) ||
                other.userAnswer == userAnswer) &&
            (identical(other.suggestedAnswers, suggestedAnswers) ||
                other.suggestedAnswers == suggestedAnswers) &&
            (identical(other.answeredAt, answeredAt) ||
                other.answeredAt == answeredAt));
  }

  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  int get hashCode => Object.hash(
      runtimeType,
      id,
      stagedEntityId,
      fieldName,
      questionText,
      answerType,
      priority,
      isAnswered,
      userAnswer,
      suggestedAnswers,
      answeredAt);

  /// Create a copy of AIQuestionModel
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$AIQuestionModelImplCopyWith<_$AIQuestionModelImpl> get copyWith =>
      __$$AIQuestionModelImplCopyWithImpl<_$AIQuestionModelImpl>(
          this, _$identity);

  @override
  Map<String, dynamic> toJson() {
    return _$$AIQuestionModelImplToJson(
      this,
    );
  }
}

abstract class _AIQuestionModel implements AIQuestionModel {
  const factory _AIQuestionModel(
      {required final int id,
      required final int stagedEntityId,
      required final String fieldName,
      required final String questionText,
      required final String answerType,
      required final String priority,
      required final bool isAnswered,
      final String? userAnswer,
      final String? suggestedAnswers,
      final String? answeredAt}) = _$AIQuestionModelImpl;

  factory _AIQuestionModel.fromJson(Map<String, dynamic> json) =
      _$AIQuestionModelImpl.fromJson;

  @override
  int get id;
  @override
  int get stagedEntityId;
  @override
  String get fieldName;
  @override
  String get questionText;
  @override
  String
      get answerType; // 'text', 'date', 'time', 'datetime', 'choice', 'number'
  @override
  String get priority; // 'critical', 'high', 'medium', 'low'
  @override
  bool get isAnswered;
  @override
  String? get userAnswer;
  @override
  String? get suggestedAnswers; // JSON array string
  @override
  String? get answeredAt;

  /// Create a copy of AIQuestionModel
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$AIQuestionModelImplCopyWith<_$AIQuestionModelImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
