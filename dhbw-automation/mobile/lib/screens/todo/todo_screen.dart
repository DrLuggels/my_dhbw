import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import 'package:pull_to_refresh/pull_to_refresh.dart';
import '../../core/models/todo.dart';
import '../../core/services/todo_service.dart';
import 'dart:async';

class TodoScreen extends StatefulWidget {
  const TodoScreen({super.key});

  @override
  State<TodoScreen> createState() => _TodoScreenState();
}

class _TodoScreenState extends State<TodoScreen> {
  List<Todo> _todos = [];
  bool _isLoading = false;
  String _filter = 'all';
  Timer? _pollTimer;
  final RefreshController _refreshController = RefreshController();

  @override
  void initState() {
    super.initState();
    _loadTodos();
    _startPolling();
  }

  @override
  void dispose() {
    _pollTimer?.cancel();
    _refreshController.dispose();
    super.dispose();
  }

  void _startPolling() {
    _pollTimer = Timer.periodic(const Duration(seconds: 30), (_) {
      _loadTodos(silent: true);
    });
  }

  Future<void> _loadTodos({bool silent = false}) async {
    if (!silent) {
      setState(() => _isLoading = true);
    }

    final todoService = context.read<TodoService>();
    final todos = await todoService.getTodos(
      status: _filter == 'active' ? 'pending' : (_filter == 'completed' ? 'completed' : null),
    );

    setState(() {
      _todos = todos;
      _isLoading = false;
    });

    _refreshController.refreshCompleted();
  }

  Future<void> _toggleTodoStatus(Todo todo) async {
    final newStatus = todo.status == 'completed' ? 'pending' : 'completed';
    
    try {
      final todoService = context.read<TodoService>();
      await todoService.updateTodoStatus(todo.id, newStatus);
      _loadTodos();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Fehler: $e')),
        );
      }
    }
  }

  Future<void> _deleteTodo(Todo todo) async {
    try {
      final todoService = context.read<TodoService>();
      await todoService.deleteTodo(todo.id);
      _loadTodos();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Fehler: $e')),
        );
      }
    }
  }

  void _showAddTodoDialog() {
    final titleController = TextEditingController();
    final descriptionController = TextEditingController();
    DateTime? dueDate;
    String category = 'general';
    int priority = 5;

    showDialog(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setDialogState) => AlertDialog(
          title: const Text('Neues Todo'),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextField(
                  controller: titleController,
                  decoration: const InputDecoration(
                    labelText: 'Titel',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: descriptionController,
                  decoration: const InputDecoration(
                    labelText: 'Beschreibung (optional)',
                    border: OutlineInputBorder(),
                  ),
                  maxLines: 3,
                ),
                const SizedBox(height: 12),
                ListTile(
                  title: Text(dueDate != null
                      ? 'Fällig: ${DateFormat('dd.MM.yyyy').format(dueDate!)}'
                      : 'Kein Fälligkeitsdatum'),
                  trailing: const Icon(Icons.calendar_today),
                  onTap: () async {
                    final picked = await showDatePicker(
                      context: context,
                      initialDate: DateTime.now(),
                      firstDate: DateTime.now(),
                      lastDate: DateTime.now().add(const Duration(days: 365)),
                    );
                    if (picked != null) {
                      setDialogState(() => dueDate = picked);
                    }
                  },
                ),
                const SizedBox(height: 8),
                DropdownButtonFormField<String>(
                  value: category,
                  decoration: const InputDecoration(
                    labelText: 'Kategorie',
                    border: OutlineInputBorder(),
                  ),
                  items: const [
                    DropdownMenuItem(value: 'general', child: Text('Allgemein')),
                    DropdownMenuItem(value: 'study', child: Text('Studium')),
                    DropdownMenuItem(value: 'assignment', child: Text('Aufgabe')),
                    DropdownMenuItem(value: 'exam', child: Text('Prüfung')),
                  ],
                  onChanged: (value) => setDialogState(() => category = value!),
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    const Text('Priorität:'),
                    Expanded(
                      child: Slider(
                        value: priority.toDouble(),
                        min: 1,
                        max: 10,
                        divisions: 9,
                        label: priority.toString(),
                        onChanged: (value) => setDialogState(() => priority = value.toInt()),
                      ),
                    ),
                    Text(priority.toString()),
                  ],
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Abbrechen'),
            ),
            ElevatedButton(
              onPressed: () async {
                if (titleController.text.isEmpty) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Bitte Titel eingeben')),
                  );
                  return;
                }

                try {
                  final todoService = context.read<TodoService>();
                  await todoService.createTodo(
                    title: titleController.text,
                    description: descriptionController.text.isEmpty ? null : descriptionController.text,
                    dueDate: dueDate,
                    category: category,
                    priority: priority,
                  );
                  
                  if (mounted) {
                    Navigator.pop(context);
                    _loadTodos();
                  }
                } catch (e) {
                  if (mounted) {
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(content: Text('Fehler: $e')),
                    );
                  }
                }
              },
              child: const Text('Erstellen'),
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final filteredTodos = _todos;
    final activeTodos = _todos.where((t) => t.status != 'completed').toList();
    final completedTodos = _todos.where((t) => t.status == 'completed').toList();

    return Scaffold(
      appBar: AppBar(
        title: const Text('Todos'),
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(48),
          child: Container(
            color: Colors.white,
            child: Row(
              children: [
                _FilterChip(
                  label: 'Alle (${_todos.length})',
                  selected: _filter == 'all',
                  onSelected: () => setState(() {
                    _filter = 'all';
                    _loadTodos();
                  }),
                ),
                _FilterChip(
                  label: 'Aktiv (${activeTodos.length})',
                  selected: _filter == 'active',
                  onSelected: () => setState(() {
                    _filter = 'active';
                    _loadTodos();
                  }),
                ),
                _FilterChip(
                  label: 'Erledigt (${completedTodos.length})',
                  selected: _filter == 'completed',
                  onSelected: () => setState(() {
                    _filter = 'completed';
                    _loadTodos();
                  }),
                ),
              ],
            ),
          ),
        ),
      ),
      body: SmartRefresher(
        controller: _refreshController,
        onRefresh: () => _loadTodos(),
        child: _isLoading && _todos.isEmpty
            ? const Center(child: CircularProgressIndicator())
            : filteredTodos.isEmpty
                ? const Center(
                    child: Text(
                      'Keine Todos',
                      style: TextStyle(color: Colors.grey),
                    ),
                  )
                : ListView.builder(
                    itemCount: filteredTodos.length,
                    itemBuilder: (context, index) {
                      final todo = filteredTodos[index];
                      return _TodoTile(
                        todo: todo,
                        onToggle: () => _toggleTodoStatus(todo),
                        onDelete: () => _deleteTodo(todo),
                      );
                    },
                  ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: _showAddTodoDialog,
        child: const Icon(Icons.add),
      ),
    );
  }
}

class _FilterChip extends StatelessWidget {
  final String label;
  final bool selected;
  final VoidCallback onSelected;

  const _FilterChip({
    required this.label,
    required this.selected,
    required this.onSelected,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 4),
      child: FilterChip(
        label: Text(label),
        selected: selected,
        onSelected: (_) => onSelected(),
      ),
    );
  }
}

class _TodoTile extends StatelessWidget {
  final Todo todo;
  final VoidCallback onToggle;
  final VoidCallback onDelete;

  const _TodoTile({
    required this.todo,
    required this.onToggle,
    required this.onDelete,
  });

  Color _getPriorityColor(int priority) {
    if (priority >= 8) return Colors.red;
    if (priority >= 5) return Colors.orange;
    return Colors.green;
  }

  @override
  Widget build(BuildContext context) {
    final isCompleted = todo.status == 'completed';
    
    return Dismissible(
      key: Key(todo.id.toString()),
      direction: DismissDirection.endToStart,
      background: Container(
        color: Colors.red,
        alignment: Alignment.centerRight,
        padding: const EdgeInsets.only(right: 16),
        child: const Icon(Icons.delete, color: Colors.white),
      ),
      onDismissed: (_) => onDelete(),
      child: Card(
        margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
        child: ListTile(
          leading: Checkbox(
            value: isCompleted,
            onChanged: (_) => onToggle(),
          ),
          title: Text(
            todo.title,
            style: TextStyle(
              decoration: isCompleted ? TextDecoration.lineThrough : null,
              fontWeight: FontWeight.bold,
            ),
          ),
          subtitle: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (todo.description != null) ...[
                const SizedBox(height: 4),
                Text(
                  todo.description!,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
              ],
              const SizedBox(height: 4),
              Row(
                children: [
                  Chip(
                    label: Text(
                      todo.category.toUpperCase(),
                      style: const TextStyle(fontSize: 10),
                    ),
                    backgroundColor: Colors.grey[200],
                    materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                  ),
                  const SizedBox(width: 8),
                  if (todo.dueDate != null) ...[
                    Icon(
                      Icons.calendar_today,
                      size: 14,
                      color: todo.isOverdue ? Colors.red : Colors.grey,
                    ),
                    const SizedBox(width: 4),
                    Text(
                      DateFormat('dd.MM.yyyy').format(todo.dueDate!),
                      style: TextStyle(
                        fontSize: 12,
                        color: todo.isOverdue ? Colors.red : Colors.grey,
                        fontWeight: todo.isOverdue ? FontWeight.bold : null,
                      ),
                    ),
                  ],
                ],
              ),
            ],
          ),
          trailing: Container(
            width: 8,
            height: 8,
            decoration: BoxDecoration(
              color: _getPriorityColor(todo.priority),
              shape: BoxShape.circle,
            ),
          ),
        ),
      ),
    );
  }
}
