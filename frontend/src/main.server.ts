import { bootstrapApplication } from '@angular/platform-browser';
import type { BootstrapContext } from '@angular/platform-browser';
import { AppServerConfig } from './app/app.config.server';
import { App } from './app/app';

/**
 * Angular 21 requires bootstrapApplication to receive the BootstrapContext
 * when called on the server. Without it Angular cannot locate the platform
 * and throws NG0401 ("Missing Platform").
 */
const bootstrap = (context: BootstrapContext) =>
  bootstrapApplication(App, AppServerConfig, context);

export default bootstrap;
