// coverage:ignore-file
// GENERATED CODE - DO NOT MODIFY BY HAND
// ignore_for_file: type=lint
// ignore_for_file: unused_element, deprecated_member_use, deprecated_member_use_from_same_package, use_function_type_syntax_for_parameters, unnecessary_const, avoid_init_to_null, invalid_override_different_default_values_named, prefer_expression_function_bodies, annotate_overrides, invalid_annotation_target, unnecessary_question_mark

part of 'validation_provider.dart';

// **************************************************************************
// FreezedGenerator
// **************************************************************************

T _$identity<T>(T value) => value;

final _privateConstructorUsedError = UnsupportedError(
    'It seems like you constructed your class using `MyClass._()`. This constructor is only meant to be used by freezed and you are not supposed to need it nor use it.\nPlease check the documentation here for more information: https://github.com/rrousselGit/freezed#adding-getters-and-methods-to-our-models');

/// @nodoc
mixin _$ValidationState {
  List<StagedEntityModel> get pendingEntities =>
      throw _privateConstructorUsedError;
  bool get isLoading => throw _privateConstructorUsedError;
  String? get error => throw _privateConstructorUsedError;
  String? get successMessage => throw _privateConstructorUsedError;

  /// Create a copy of ValidationState
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  $ValidationStateCopyWith<ValidationState> get copyWith =>
      throw _privateConstructorUsedError;
}

/// @nodoc
abstract class $ValidationStateCopyWith<$Res> {
  factory $ValidationStateCopyWith(
          ValidationState value, $Res Function(ValidationState) then) =
      _$ValidationStateCopyWithImpl<$Res, ValidationState>;
  @useResult
  $Res call(
      {List<StagedEntityModel> pendingEntities,
      bool isLoading,
      String? error,
      String? successMessage});
}

/// @nodoc
class _$ValidationStateCopyWithImpl<$Res, $Val extends ValidationState>
    implements $ValidationStateCopyWith<$Res> {
  _$ValidationStateCopyWithImpl(this._value, this._then);

  // ignore: unused_field
  final $Val _value;
  // ignore: unused_field
  final $Res Function($Val) _then;

  /// Create a copy of ValidationState
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? pendingEntities = null,
    Object? isLoading = null,
    Object? error = freezed,
    Object? successMessage = freezed,
  }) {
    return _then(_value.copyWith(
      pendingEntities: null == pendingEntities
          ? _value.pendingEntities
          : pendingEntities // ignore: cast_nullable_to_non_nullable
              as List<StagedEntityModel>,
      isLoading: null == isLoading
          ? _value.isLoading
          : isLoading // ignore: cast_nullable_to_non_nullable
              as bool,
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
}

/// @nodoc
abstract class _$$ValidationStateImplCopyWith<$Res>
    implements $ValidationStateCopyWith<$Res> {
  factory _$$ValidationStateImplCopyWith(_$ValidationStateImpl value,
          $Res Function(_$ValidationStateImpl) then) =
      __$$ValidationStateImplCopyWithImpl<$Res>;
  @override
  @useResult
  $Res call(
      {List<StagedEntityModel> pendingEntities,
      bool isLoading,
      String? error,
      String? successMessage});
}

/// @nodoc
class __$$ValidationStateImplCopyWithImpl<$Res>
    extends _$ValidationStateCopyWithImpl<$Res, _$ValidationStateImpl>
    implements _$$ValidationStateImplCopyWith<$Res> {
  __$$ValidationStateImplCopyWithImpl(
      _$ValidationStateImpl _value, $Res Function(_$ValidationStateImpl) _then)
      : super(_value, _then);

  /// Create a copy of ValidationState
  /// with the given fields replaced by the non-null parameter values.
  @pragma('vm:prefer-inline')
  @override
  $Res call({
    Object? pendingEntities = null,
    Object? isLoading = null,
    Object? error = freezed,
    Object? successMessage = freezed,
  }) {
    return _then(_$ValidationStateImpl(
      pendingEntities: null == pendingEntities
          ? _value._pendingEntities
          : pendingEntities // ignore: cast_nullable_to_non_nullable
              as List<StagedEntityModel>,
      isLoading: null == isLoading
          ? _value.isLoading
          : isLoading // ignore: cast_nullable_to_non_nullable
              as bool,
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

class _$ValidationStateImpl extends _ValidationState {
  const _$ValidationStateImpl(
      {final List<StagedEntityModel> pendingEntities = const [],
      this.isLoading = false,
      this.error,
      this.successMessage})
      : _pendingEntities = pendingEntities,
        super._();

  final List<StagedEntityModel> _pendingEntities;
  @override
  @JsonKey()
  List<StagedEntityModel> get pendingEntities {
    if (_pendingEntities is EqualUnmodifiableListView) return _pendingEntities;
    // ignore: implicit_dynamic_type
    return EqualUnmodifiableListView(_pendingEntities);
  }

  @override
  @JsonKey()
  final bool isLoading;
  @override
  final String? error;
  @override
  final String? successMessage;

  @override
  String toString() {
    return 'ValidationState(pendingEntities: $pendingEntities, isLoading: $isLoading, error: $error, successMessage: $successMessage)';
  }

  @override
  bool operator ==(Object other) {
    return identical(this, other) ||
        (other.runtimeType == runtimeType &&
            other is _$ValidationStateImpl &&
            const DeepCollectionEquality()
                .equals(other._pendingEntities, _pendingEntities) &&
            (identical(other.isLoading, isLoading) ||
                other.isLoading == isLoading) &&
            (identical(other.error, error) || other.error == error) &&
            (identical(other.successMessage, successMessage) ||
                other.successMessage == successMessage));
  }

  @override
  int get hashCode => Object.hash(
      runtimeType,
      const DeepCollectionEquality().hash(_pendingEntities),
      isLoading,
      error,
      successMessage);

  /// Create a copy of ValidationState
  /// with the given fields replaced by the non-null parameter values.
  @JsonKey(includeFromJson: false, includeToJson: false)
  @override
  @pragma('vm:prefer-inline')
  _$$ValidationStateImplCopyWith<_$ValidationStateImpl> get copyWith =>
      __$$ValidationStateImplCopyWithImpl<_$ValidationStateImpl>(
          this, _$identity);
}

abstract class _ValidationState extends ValidationState {
  const factory _ValidationState(
      {final List<StagedEntityModel> pendingEntities,
      final bool isLoading,
      final String? error,
      final String? successMessage}) = _$ValidationStateImpl;
  const _ValidationState._() : super._();

  @override
  List<StagedEntityModel> get pendingEntities;
  @override
  bool get isLoading;
  @override
  String? get error;
  @override
  String? get successMessage;

  /// Create a copy of ValidationState
  /// with the given fields replaced by the non-null parameter values.
  @override
  @JsonKey(includeFromJson: false, includeToJson: false)
  _$$ValidationStateImplCopyWith<_$ValidationStateImpl> get copyWith =>
      throw _privateConstructorUsedError;
}
