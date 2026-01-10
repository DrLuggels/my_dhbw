// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'user_model.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

_$UserModelImpl _$$UserModelImplFromJson(Map<String, dynamic> json) =>
    _$UserModelImpl(
      id: (json['id'] as num).toInt(),
      email: json['email'] as String,
      firstName: json['firstName'] as String,
      lastName: json['lastName'] as String,
      matriculationNumber: json['matriculationNumber'] as String?,
      course: json['course'] as String?,
      emailVerified: json['emailVerified'] as bool?,
      createdAt: json['createdAt'] as String?,
    );

Map<String, dynamic> _$$UserModelImplToJson(_$UserModelImpl instance) =>
    <String, dynamic>{
      'id': instance.id,
      'email': instance.email,
      'firstName': instance.firstName,
      'lastName': instance.lastName,
      'matriculationNumber': instance.matriculationNumber,
      'course': instance.course,
      'emailVerified': instance.emailVerified,
      'createdAt': instance.createdAt,
    };
