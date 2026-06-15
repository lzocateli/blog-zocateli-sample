import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';

import { DashboardComponent } from './app/features/dashboard/dashboard.component';
import { appConfig } from './app/app.config';
import { appRoutes } from './app/app.routes';

bootstrapApplication(DashboardComponent, {
  ...appConfig,
  providers: [...(appConfig.providers ?? []), provideRouter(appRoutes)],
}).catch((err) => console.error(err));
