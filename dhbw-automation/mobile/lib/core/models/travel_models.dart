class TrainConnectionRequest {
  final String from;
  final String to;
  final DateTime? dateTime;
  final int maxConnections;

  TrainConnectionRequest({
    this.from = "Laupheim West",
    this.to = "Ravensburg",
    this.dateTime,
    this.maxConnections = 5,
  });

  Map<String, dynamic> toJson() => {
        'from': from,
        'to': to,
        'dateTime': dateTime?.toIso8601String(),
        'maxConnections': maxConnections,
      };
}

class TrainConnectionResponse {
  final List<Journey> journeys;
  final DateTime requestedAt;

  TrainConnectionResponse({
    required this.journeys,
    required this.requestedAt,
  });

  factory TrainConnectionResponse.fromJson(Map<String, dynamic> json) {
    return TrainConnectionResponse(
      journeys: (json['journeys'] as List<dynamic>?)
              ?.map((j) => Journey.fromJson(j as Map<String, dynamic>))
              .toList() ??
          [],
      requestedAt: DateTime.parse(json['requestedAt'] as String),
    );
  }
}

class Journey {
  final String from;
  final String to;
  final DateTime departure;
  final DateTime arrival;
  final String duration;
  final int transfers;
  final List<Leg> legs;
  final bool? cancelled;
  final int? delay;

  Journey({
    required this.from,
    required this.to,
    required this.departure,
    required this.arrival,
    required this.duration,
    required this.transfers,
    required this.legs,
    this.cancelled,
    this.delay,
  });

  factory Journey.fromJson(Map<String, dynamic> json) {
    return Journey(
      from: json['from'] as String,
      to: json['to'] as String,
      departure: DateTime.parse(json['departure'] as String),
      arrival: DateTime.parse(json['arrival'] as String),
      duration: json['duration'] as String,
      transfers: json['transfers'] as int,
      legs: (json['legs'] as List<dynamic>?)
              ?.map((l) => Leg.fromJson(l as Map<String, dynamic>))
              .toList() ??
          [],
      cancelled: json['cancelled'] as bool?,
      delay: json['delay'] as int?,
    );
  }
}

class Leg {
  final String from;
  final String to;
  final DateTime departure;
  final DateTime arrival;
  final String? line;
  final String? direction;
  final String? platform;
  final int? delay;
  final bool? cancelled;

  Leg({
    required this.from,
    required this.to,
    required this.departure,
    required this.arrival,
    this.line,
    this.direction,
    this.platform,
    this.delay,
    this.cancelled,
  });

  factory Leg.fromJson(Map<String, dynamic> json) {
    return Leg(
      from: json['from'] as String,
      to: json['to'] as String,
      departure: DateTime.parse(json['departure'] as String),
      arrival: DateTime.parse(json['arrival'] as String),
      line: json['line'] as String?,
      direction: json['direction'] as String?,
      platform: json['platform'] as String?,
      delay: json['delay'] as int?,
      cancelled: json['cancelled'] as bool?,
    );
  }
}
