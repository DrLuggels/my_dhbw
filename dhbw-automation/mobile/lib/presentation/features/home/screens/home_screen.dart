import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../calendar/screens/calendar_screen.dart';
import '../../files/screens/files_screen.dart';
import '../../validation/screens/validation_screen.dart';
import '../../learning/screens/learning_screen.dart';
import '../../auth/providers/auth_provider.dart';

/// Home Screen with Bottom Navigation
/// Provides access to all main features:
/// - Calendar (week view)
/// - Files (upload and management)
/// - Validation (answer questions)
/// - Learning (exercises)
class HomeScreen extends ConsumerStatefulWidget {
  const HomeScreen({super.key});

  @override
  ConsumerState<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends ConsumerState<HomeScreen> {
  int _selectedIndex = 0;

  final List<Widget> _screens = const [
    CalendarScreen(),
    FilesScreen(),
    ValidationScreen(),
    LearningScreen(),
  ];

  final List<_NavItem> _navItems = const [
    _NavItem(
      icon: Icons.calendar_today,
      label: 'Kalender',
    ),
    _NavItem(
      icon: Icons.folder,
      label: 'Dateien',
    ),
    _NavItem(
      icon: Icons.help_outline,
      label: 'Rückfragen',
    ),
    _NavItem(
      icon: Icons.school,
      label: 'Übungen',
    ),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(
        index: _selectedIndex,
        children: _screens,
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _selectedIndex,
        onTap: (index) => setState(() => _selectedIndex = index),
        type: BottomNavigationBarType.fixed,
        selectedItemColor: Theme.of(context).primaryColor,
        unselectedItemColor: Colors.grey,
        selectedFontSize: 12,
        unselectedFontSize: 12,
        items: _navItems
            .map((item) => BottomNavigationBarItem(
                  icon: Icon(item.icon),
                  label: item.label,
                ))
            .toList(),
      ),
      drawer: _AppDrawer(),
    );
  }
}

class _NavItem {
  final IconData icon;
  final String label;

  const _NavItem({
    required this.icon,
    required this.label,
  });
}

/// App Drawer with user info and logout
class _AppDrawer extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(authProvider).user;

    return Drawer(
      child: ListView(
        padding: EdgeInsets.zero,
        children: [
          UserAccountsDrawerHeader(
            decoration: BoxDecoration(
              color: Theme.of(context).primaryColor,
            ),
            currentAccountPicture: CircleAvatar(
              backgroundColor: Colors.white,
              child: Text(
                user != null
                    ? '${user.firstName[0]}${user.lastName[0]}'
                    : 'U',
                style: TextStyle(
                  fontSize: 24,
                  fontWeight: FontWeight.bold,
                  color: Theme.of(context).primaryColor,
                ),
              ),
            ),
            accountName: user != null
                ? Text('${user.firstName} ${user.lastName}')
                : const Text('Benutzer'),
            accountEmail: user != null ? Text(user.email) : null,
          ),
          ListTile(
            leading: const Icon(Icons.person),
            title: const Text('Profil'),
            onTap: () {
              Navigator.pop(context);
              // TODO: Navigate to profile screen
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Profil-Ansicht kommt bald')),
              );
            },
          ),
          ListTile(
            leading: const Icon(Icons.settings),
            title: const Text('Einstellungen'),
            onTap: () {
              Navigator.pop(context);
              // TODO: Navigate to settings screen
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(content: Text('Einstellungen kommen bald')),
              );
            },
          ),
          const Divider(),
          ListTile(
            leading: const Icon(Icons.info_outline),
            title: const Text('Über'),
            onTap: () {
              Navigator.pop(context);
              showAboutDialog(
                context: context,
                applicationName: 'DHBW Automation',
                applicationVersion: '1.0.0',
                applicationLegalese: '© 2025 DHBW Automation Team',
                children: [
                  const SizedBox(height: 16),
                  const Text(
                    'Mobile App für die DHBW Automation Platform mit '
                    'Features wie Kalender, Datei-Upload, Rückfragen und '
                    'Übungsaufgaben.',
                  ),
                ],
              );
            },
          ),
          const Divider(),
          ListTile(
            leading: const Icon(Icons.logout, color: Colors.red),
            title: const Text('Abmelden', style: TextStyle(color: Colors.red)),
            onTap: () async {
              Navigator.pop(context);
              final confirmed = await showDialog<bool>(
                context: context,
                builder: (context) => AlertDialog(
                  title: const Text('Abmelden?'),
                  content: const Text(
                    'Möchtest du dich wirklich abmelden?',
                  ),
                  actions: [
                    TextButton(
                      onPressed: () => Navigator.pop(context, false),
                      child: const Text('Abbrechen'),
                    ),
                    TextButton(
                      onPressed: () => Navigator.pop(context, true),
                      style: TextButton.styleFrom(foregroundColor: Colors.red),
                      child: const Text('Abmelden'),
                    ),
                  ],
                ),
              );

              if (confirmed == true && context.mounted) {
                await ref.read(authProvider.notifier).logout();
                // Navigation handled by main.dart auth state listener
              }
            },
          ),
        ],
      ),
    );
  }
}
