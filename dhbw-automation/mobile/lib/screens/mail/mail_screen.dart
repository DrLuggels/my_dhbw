import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import 'package:pull_to_refresh/pull_to_refresh.dart';
import '../../core/models/email.dart';
import '../../core/services/mail_service.dart';
import 'dart:async';

class MailScreen extends StatefulWidget {
  const MailScreen({super.key});

  @override
  State<MailScreen> createState() => _MailScreenState();
}

class _MailScreenState extends State<MailScreen> {
  List<Email> _emails = [];
  bool _isLoading = false;
  String _filter = 'all';
  Timer? _pollTimer;
  final RefreshController _refreshController = RefreshController();

  @override
  void initState() {
    super.initState();
    _loadEmails();
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
      _loadEmails(silent: true);
    });
  }

  Future<void> _loadEmails({bool silent = false}) async {
    if (!silent) {
      setState(() => _isLoading = true);
    }

    final mailService = context.read<MailService>();
    final emails = await mailService.getInbox(
      isRead: _filter == 'unread' ? false : null,
      requiresAction: _filter == 'action' ? true : null,
    );

    setState(() {
      _emails = emails;
      _isLoading = false;
    });

    _refreshController.refreshCompleted();
  }

  Future<void> _syncMail() async {
    try {
      final mailService = context.read<MailService>();
      await mailService.syncMail();
      
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Emails synchronisiert')),
        );
        _loadEmails();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Fehler: $e')),
        );
      }
    }
  }

  Future<void> _markAsRead(Email email) async {
    try {
      final mailService = context.read<MailService>();
      await mailService.markAsRead(email.id);
      _loadEmails();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Fehler: $e')),
        );
      }
    }
  }

  Future<void> _performAction(Email email, String action) async {
    try {
      final mailService = context.read<MailService>();
      await mailService.performAction(email.id, action);
      
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Aktion "$action" ausgeführt')),
        );
        _loadEmails();
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Fehler: $e')),
        );
      }
    }
  }

  void _showEmailActions(Email email) {
    showModalBottomSheet(
      context: context,
      builder: (context) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (!email.isRead)
              ListTile(
                leading: const Icon(Icons.mark_email_read),
                title: const Text('Als gelesen markieren'),
                onTap: () {
                  Navigator.pop(context);
                  _markAsRead(email);
                },
              ),
            if (email.requiresAction) ...[
              ListTile(
                leading: const Icon(Icons.check_circle, color: Colors.green),
                title: const Text('Akzeptieren'),
                onTap: () {
                  Navigator.pop(context);
                  _performAction(email, 'accept');
                },
              ),
              ListTile(
                leading: const Icon(Icons.cancel, color: Colors.red),
                title: const Text('Ablehnen'),
                onTap: () {
                  Navigator.pop(context);
                  _performAction(email, 'decline');
                },
              ),
              ListTile(
                leading: const Icon(Icons.snooze, color: Colors.orange),
                title: const Text('Später erinnern'),
                onTap: () {
                  Navigator.pop(context);
                  _performAction(email, 'snooze');
                },
              ),
            ],
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final unreadCount = _emails.where((e) => !e.isRead).length;
    final actionCount = _emails.where((e) => e.requiresAction).length;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Emails'),
        actions: [
          IconButton(
            icon: const Icon(Icons.sync),
            onPressed: _syncMail,
            tooltip: 'Emails synchronisieren',
          ),
        ],
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(48),
          child: Container(
            color: Colors.white,
            child: Row(
              children: [
                _FilterChip(
                  label: 'Alle (${_emails.length})',
                  selected: _filter == 'all',
                  onSelected: () => setState(() {
                    _filter = 'all';
                    _loadEmails();
                  }),
                ),
                _FilterChip(
                  label: 'Ungelesen ($unreadCount)',
                  selected: _filter == 'unread',
                  onSelected: () => setState(() {
                    _filter = 'unread';
                    _loadEmails();
                  }),
                ),
                _FilterChip(
                  label: 'Aktion erforderlich ($actionCount)',
                  selected: _filter == 'action',
                  onSelected: () => setState(() {
                    _filter = 'action';
                    _loadEmails();
                  }),
                ),
              ],
            ),
          ),
        ),
      ),
      body: SmartRefresher(
        controller: _refreshController,
        onRefresh: () => _loadEmails(),
        child: _isLoading && _emails.isEmpty
            ? const Center(child: CircularProgressIndicator())
            : _emails.isEmpty
                ? const Center(
                    child: Text(
                      'Keine Emails',
                      style: TextStyle(color: Colors.grey),
                    ),
                  )
                : ListView.builder(
                    itemCount: _emails.length,
                    itemBuilder: (context, index) {
                      final email = _emails[index];
                      return _EmailTile(
                        email: email,
                        onTap: () => _showEmailActions(email),
                      );
                    },
                  ),
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

class _EmailTile extends StatelessWidget {
  final Email email;
  final VoidCallback onTap;

  const _EmailTile({
    required this.email,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      color: email.isRead ? Colors.white : Colors.blue[50],
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: email.isImportant ? Colors.red : Colors.blue,
          child: Text(
            email.from.substring(0, 1).toUpperCase(),
            style: const TextStyle(color: Colors.white),
          ),
        ),
        title: Row(
          children: [
            Expanded(
              child: Text(
                email.from,
                style: TextStyle(
                  fontWeight: email.isRead ? FontWeight.normal : FontWeight.bold,
                ),
                overflow: TextOverflow.ellipsis,
              ),
            ),
            Text(
              DateFormat('HH:mm').format(email.receivedAt),
              style: TextStyle(
                fontSize: 12,
                color: Colors.grey[600],
              ),
            ),
          ],
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 4),
            Text(
              email.subject,
              style: TextStyle(
                fontWeight: email.isRead ? FontWeight.normal : FontWeight.bold,
              ),
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
            if (email.preview != null) ...[
              const SizedBox(height: 4),
              Text(
                email.preview!,
                style: const TextStyle(fontSize: 13, color: Colors.grey),
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
            ],
            const SizedBox(height: 4),
            Row(
              children: [
                if (email.isImportant)
                  Container(
                    margin: const EdgeInsets.only(right: 8),
                    child: const Icon(Icons.priority_high, size: 16, color: Colors.red),
                  ),
                if (email.requiresAction)
                  Chip(
                    label: const Text(
                      'AKTION ERFORDERLICH',
                      style: TextStyle(fontSize: 10),
                    ),
                    backgroundColor: Colors.orange[100],
                    materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                  ),
              ],
            ),
          ],
        ),
        onTap: onTap,
      ),
    );
  }
}
