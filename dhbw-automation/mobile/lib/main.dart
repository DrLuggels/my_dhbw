import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'data/local/hive_boxes.dart';
import 'core/theme/app_theme.dart';
import 'presentation/features/auth/screens/login_screen.dart';
import 'presentation/features/auth/screens/register_screen.dart';
import 'presentation/features/auth/providers/auth_provider.dart';
import 'presentation/features/home/screens/home_screen.dart';

void main() async {
  // Ensure Flutter is initialized
  WidgetsFlutterBinding.ensureInitialized();

  // Initialize Hive for offline storage
  await HiveBoxes().init();

  // Run app with Riverpod ProviderScope
  runApp(
    const ProviderScope(
      child: MyApp(),
    ),
  );
}

class MyApp extends ConsumerWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authProvider);

    return MaterialApp(
      title: 'DHBW Automation',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.lightTheme,
      // Show login if not authenticated, otherwise show home with bottom nav
      home: authState.isAuthenticated ? const HomeScreen() : const LoginScreen(),
      routes: {
        '/login': (context) => const LoginScreen(),
        '/register': (context) => const RegisterScreen(),
        '/home': (context) => const HomeScreen(),
      },
    );
  }
}
