import {
  AppBar,
  Box,
  Button,
  Divider,
  Drawer,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
} from '@mui/material';
import BusinessIcon from '@mui/icons-material/Business';
import ChatIcon from '@mui/icons-material/Chat';
import ConfirmationNumberIcon from '@mui/icons-material/ConfirmationNumber';
import DashboardIcon from '@mui/icons-material/Dashboard';
import DescriptionIcon from '@mui/icons-material/Description';
import GroupIcon from '@mui/icons-material/Group';
import LogoutIcon from '@mui/icons-material/Logout';
import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../features/auth/useAuth';

const drawerWidth = 240;

const ADMIN_ROLES = ['SuperAdmin', 'CompanyAdmin'];

const navItems = [
  { label: 'Dashboard', path: '/dashboard', icon: <DashboardIcon />, adminOnly: false },
  { label: 'Empresas', path: '/companies', icon: <BusinessIcon />, adminOnly: true },
  { label: 'Usuarios', path: '/users', icon: <GroupIcon />, adminOnly: true },
  { label: 'Documentos', path: '/documents', icon: <DescriptionIcon />, adminOnly: false },
  { label: 'Chat IA', path: '/chat', icon: <ChatIcon />, adminOnly: false },
  { label: 'Tickets', path: '/tickets', icon: <ConfirmationNumberIcon />, adminOnly: false },
];

export function PrivateLayout() {
  const { user, logout } = useAuth();
  const isAdmin = !!user && ADMIN_ROLES.includes(user.role);
  const visibleNavItems = navItems.filter((item) => !item.adminOnly || isAdmin);

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <AppBar position="fixed" sx={{ zIndex: (theme) => theme.zIndex.drawer + 1 }}>
        <Toolbar sx={{ display: 'flex', justifyContent: 'space-between' }}>
          <Typography variant="h6" component="div">
            ContactCenterAI
          </Typography>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Box sx={{ textAlign: 'right' }}>
              <Typography variant="body2">{user?.email}</Typography>
              <Typography variant="caption" color="inherit" sx={{ opacity: 0.85 }}>
                {user?.role}
              </Typography>
            </Box>
            <Button color="inherit" startIcon={<LogoutIcon />} onClick={logout}>
              Cerrar sesión
            </Button>
          </Box>
        </Toolbar>
      </AppBar>

      <Drawer
        variant="permanent"
        sx={{
          width: drawerWidth,
          flexShrink: 0,
          [`& .MuiDrawer-paper`]: {
            width: drawerWidth,
            boxSizing: 'border-box',
          },
        }}
      >
        <Toolbar />
        <Divider />
        <List>
          {visibleNavItems.map((item) => (
            <ListItemButton
              key={item.path}
              component={NavLink}
              to={item.path}
              sx={{
                '&.active': {
                  bgcolor: 'action.selected',
                },
              }}
            >
              <ListItemIcon>{item.icon}</ListItemIcon>
              <ListItemText primary={item.label} />
            </ListItemButton>
          ))}
        </List>
      </Drawer>

      <Box component="main" sx={{ flexGrow: 1, p: 3 }}>
        <Toolbar />
        <Outlet />
      </Box>
    </Box>
  );
}
