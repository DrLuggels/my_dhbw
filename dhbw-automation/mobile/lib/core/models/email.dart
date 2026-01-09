class Email {
  final int id;
  final String from;
  final String subject;
  final String? preview;
  final DateTime receivedAt;
  final bool isRead;
  final bool requiresAction;
  final int priority;

  Email({
    required this.id,
    required this.from,
    required this.subject,
    this.preview,
    required this.receivedAt,
    required this.isRead,
    required this.requiresAction,
    required this.priority,
  });

  factory Email.fromJson(Map<String, dynamic> json) {
    return Email(
      id: json['id'],
      from: json['from'],
      subject: json['subject'],
      preview: json['preview'],
      receivedAt: DateTime.parse(json['receivedAt']),
      isRead: json['isRead'] ?? false,
      requiresAction: json['requiresAction'] ?? false,
      priority: json['priority'] ?? 5,
    );
  }

  bool get isImportant => priority >= 7;
}
