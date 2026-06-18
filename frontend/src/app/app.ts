import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SchemeDiscoveryModals } from './core/components/scheme-discovery-modals/scheme-discovery-modals';
import { MsmeSaathiChat } from './core/components/msme-saathi-chat/msme-saathi-chat';
import { ToastContainer } from './core/components/toast-container/toast-container';
import { Header } from "./header/header";
import { Footer } from "./footer/footer";

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css',
  imports: [Header, Footer, RouterOutlet, ToastContainer, SchemeDiscoveryModals, MsmeSaathiChat]
})
export class App {}
