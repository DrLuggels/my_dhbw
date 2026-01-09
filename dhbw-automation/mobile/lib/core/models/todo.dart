class Todo {
  final int id;
  final String title;
  final String? description;
  final DateTime? dueDate;
  final String status;
  final String category;
  final int priority;
  final DateTime createdAt;

  Todo({
    required this.id,
    required this.title,
    this.description,
    this.dueDate,
    required this.status,
    required this.category,
    required this.priority,
    required this.createdAt,
  });

  factory Todo.fromJson(Map<String, dynamic> json) {
    return Todo(
      id: json['id'],
      title: json['title'],
      description: json['description'],
      dueDate: json['dueDate'] != null ? DateTime.parse(json['dueDate']) : null,
      status: json['status'],
      category: json['category'],
      priority: json['priority'],
      createdAt: DateTime.parse(json['createdAt']),
    );
  }

  bool get isOverdue => 
      dueDate != null && dueDate!.isBefore(DateTime.now()) && status != 'completed';
}
