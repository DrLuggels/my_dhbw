// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'learning_provider.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
    'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models');

/// @nodoc
mixin _$LearningState {
  List<ExerciseModel> get exercises => throw _privateConstructorUsedError;
  bool get isLoading => throw _privateConstructorUsedError;
  bool get isSubmitting => throw _privateConstructorUsedError;
  ExerciseModel? get currentExercise => throw _privateConstructorUsedError;
  String? get error => throw _privateConstructorUsedError;
  String? get successMessage => throw _privateConstructorUsedError;

  /// Create a copy of LearningState
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $LearningStateCopyWith<LearningState> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $LearningStateCopyWith<$Res> {
  factory $LearningStateCopyWith(
          LearningState value, $Res Function(LearningState) then) =
      _$LearningStateCopyWithImpl<$Res, LearningState>;
  @useResult
  $Res call(
      {List<ExerciseModel> exercises,
      bool isLoading,
      bool isSubmitting,
      ExerciseModel? currentExercise,
      String? error,
      String? successMessage});

  $ExerciseModelCopyWith<$Res>? get currentExercise;
}

/// @nodoc
class _$LearningStateCopyWithImpl<$Res, $Val extends LearningState>
    implements $LearningStateCopyWith<$Res> {
  _$LearningStateCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of LearningState
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? exercises = null,
    Object? isLoading = null,
    Object? isSubmitting = null,
    Object? currentExercise = freezed,
    Object? error = freezed,
    Object? successMessage = freezed,
  }) {
    return _then(_value.copyWith(
      exercises: null == exercises
          ? _value.exercises
          : exercises // ignore: cast_nullable_to_non_nullable
              as List<ExerciseModel>,
      isLoading: null == isLoading
          ? _value.isLoading
          : isLoading // ignore: cast_nullable_to_non_nullable
              as bool,
      isSubmitting: null == isSubmitting
          ? _value.isSubmitting
          : isSubmitting // ignore: cast_nullable_to_non_nullable
              as bool,
      currentExercise: freezed == currentExercise
          ? _value.currentExercise
          : currentExercise // ignore: cast_nullable_to_non_nullable
              as ExerciseModel?,
      error: freezed == error
          ? _value.error
          : error // ignore: cast_nullable_to_non_nullable
              as String?,
      successMessage: freezed == successMessage
          ? _value.successMessage
          : successMessage // ignore: cast_nullable_to_non_nullable
              as String?,
    ) as $Val);
  }

  /// Create a copy of LearningState
  /// with the given fields replaced by the non-null parameter values.
  @override
  @pragma('vm:prefer-inline')
  $ExerciseModelCopyWith<$Res>? get currentExercise {
    if (_value.currentExercise == null) {
      return null;
    }

    return $ExerciseModelCopyWith<$Res>(_value.currentExercise!, (value) {
      return _then(_value.copyWith(currentExercise: value) as $Val);
    });
  }
}

/// @nodoc
abstract class _$$LearningStateImplCopyWith<$Res>
    implements $LearningStateCopyWith<$Res> {
  factory _$$LearningStateImplCopyWith(
          _$LearningStateImpl value, $Res Function(_$LearningStateImpl) then) =
      __$$LearningStateImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call(
      {List<ExerciseModel> exercises,
      bool isLoading,
      bool isSubmitting,
      ExerciseModel? currentExercise,
      String? error,
      String? successMessage});

  @override
  $ExerciseModelCopyWith<$Res>? get currentExercise;
}

/// @nodoc
class __$$LearningStateImplCopyWithImpl<$Res>
    extends _$LearningStateCopyWithImpl<$Res, _$LearningStateImpl>
    implements _$$LearningStateImplCopyWith<$Res> {
  __$$LearningStateImplCopyWithImpl(
      _$LearningStateImpl _value, $Res Function(_$LearningStateImpl) _then)
      : super(_value, _then);

  /// Create a copy of LearningState
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? exercises = null,
    Object? isLoading = null,
    Object? isSubmitting = null,
    Object? currentExercise = freezed,
    Object? error = freezed,
    Object? successMessage = freezed,
  }) {
    return _then(_$LearningStateImpl(
      exercises: null == exercises
          ? _value._exercises
          : exercises // ignore: cast_nullable_to_non_nullable
              as List<ExerciseModel>,
      isLoading: null == isLoading
          ? _value.isLoading
          : isLoading // ignore: cast_nullable_to_non_nullable
              as bool,
      isSubmitting: null == isSubmitting
          ? _value.isSubmitting
          : isSubmitting // ignore: cast_nullable_to_non_nullable
              as bool,
      currentExercise: freezed == currentExercise
          ? _value.currentExercise
          : currentExercise // ignore: cast_nullable_to_non_nullable
              as ExerciseModel?,
      error: freezed == error
          ? _value.error
          : error // ignore: cast_nullable_to_non_nullable
              as String?,
      successMessage: freezed == successMessage
          ? _value.successMessage
          : successMessage // ignore: cast_nullable_to_non_nullable
              as String?,
    ));
  }
}

/// @nodoc

class _$LearningStateImpl extends _LearningState {
  const _$LearningStateImpl(
      {final List<ExerciseModel> exercises = const [],
      this.isLoading = false,
      this.isSubmitting = false,
      this.currentExercise,
      this.error,
      this.successMessage})
      : _exercises = exercises,
        super._();

  final List<ExerciseModel> _exercises;
  @override
  @JsonKey()
  List<ExerciseModel> get exercises {
    if (_exercises is EqualUnmodifiableListView) return _exercises;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_exercises);
  }

  @override
  @JsonKey()
  final bool isLoading;
  @override
  @JsonKey()
  final bool isSubmitting;
  @override
  final ExerciseModel? currentExercise;
  @override
  final String? error;
  @override
  final String? successMessage;

  @override
  String toString() {
    return 'LearningState(exercises: $exercises, isLoading: $isLoading, isSubmitting: $isSubmitting, currentExercise: $currentExercise, error: $error, successMessage: $successMessage)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$LearningStateImpl &&
            const DeepCollectionEquality()
                .equals(other._exercises, _exercises) &&
            (identical(other.isLoading, isLoading) ||
                other.isLoading == isLoading) &&
            (identical(other.isSubmitting, isSubmitting) ||
                other.isSubmitting == isSubmitting) &&
            (identical(other.currentExercise, currentExercise) ||
                other.currentExercise == currentExercise) &&
            (identical(other.error, error) || other.error == error) &&
            (identical(other.successMessage, successMessage) ||
                other.successMessage == successMessage));
  }

  @override
  int get hashCode => Object.hash(
      runtimeType,
      const DeepCollectionEquality().hash(_exercises),
      isLoading,
      isSubmitting,
      currentExercise,
      error,
      successMessage);

  /// Create a copy of LearningState
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$LearningStateImplCopyWith<_$LearningStateImpl> get copyWith =>
      __$$LearningStateImplCopyWithImpl<_$LearningStateImpl>(this, _$identity);
}

abstract class _LearningState extends LearningState {
  const factory _LearningState(
      {final List<ExerciseModel> exercises,
      final bool isLoading,
      final bool isSubmitting,
      final ExerciseModel? currentExercise,
      final String? error,
      final String? successMessage}) = _$LearningStateImpl;
  const _LearningState._() : super._();

  @override
  List<ExerciseModel> get exercises;
  @override
  bool get isLoading;
  @override
  bool get isSubmitting;
  @override
  ExerciseModel? get currentExercise;
  @override
  String? get error;
  @override
  String? get successMessage;

  /// Create a copy of LearningState
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$LearningStateImplCopyWith<_$LearningStateImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
