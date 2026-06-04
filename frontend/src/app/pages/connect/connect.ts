import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom, timeout } from 'rxjs';
import { AuthSessionService } from '../../core/services/auth-session.service';
import { PaymentService, type ActivePlan } from '../../core/services/payment.service';
import { ToastService } from '../../core/services/toast.service';

type Msme = {
  id: number;
  name: string;
  initials: string;
  type: string;
  sector: string;
  state: string;
  city: string;
  turnover: string;
  udyam: string;
  employees: string;
  desc: string;
  contact: string;
  email: string;
  website: string;
  established: string;
  verified: boolean;
};

const MSME_DATA: Msme[] = [
  {id:1,name:'Kumar Enterprises',initials:'KE',type:'Manufacturer',sector:'Manufacturing',state:'Maharashtra',city:'Mumbai',turnover:'₹1 Cr – ₹5 Cr',udyam:'UDYAM-MH-00-1234567',employees:'25-50',desc:'Precision engineering components supplier for auto and industrial OEMs. ISO 9001 certified.',contact:'+91 9800000001',email:'info@kumarenterprises.com',website:'kumarenterprises.com',established:'2010',verified:true},
  {id:2,name:'Sharma Textiles',initials:'ST',type:'Manufacturer',sector:'Textile & Apparel',state:'Gujarat',city:'Surat',turnover:'₹50L – ₹1 Cr',udyam:'UDYAM-GJ-00-2345678',employees:'10-25',desc:'Export-quality synthetic fabric manufacturer serving retail brands and garment exporters across India.',contact:'+91 9800000002',email:'contact@sharmatextiles.in',website:'sharmatextiles.in',established:'2015',verified:true},
  {id:3,name:'Patel Agro Foods',initials:'PA',type:'Manufacturer',sector:'Food & Agriculture',state:'Gujarat',city:'Ahmedabad',turnover:'₹10L – ₹50L',udyam:'UDYAM-GJ-00-3456789',employees:'5-10',desc:'Organic spices and pulses processing unit with FSSAI certification and PAN India distribution reach.',contact:'+91 9800000003',email:'sales@patelagro.com',website:'patelagro.com',established:'2018',verified:false},
  {id:4,name:'TechSoft Solutions',initials:'TS',type:'Service Provider',sector:'Technology',state:'Karnataka',city:'Bengaluru',turnover:'₹1 Cr – ₹5 Cr',udyam:'UDYAM-KA-00-4567890',employees:'25-50',desc:'ERP and custom software development for MSMEs. Specialised in manufacturing and retail automation.',contact:'+91 9800000004',email:'hello@techsoft.in',website:'techsoft.in',established:'2012',verified:true},
  {id:5,name:'Mehta Steel Works',initials:'MS',type:'Manufacturer',sector:'Manufacturing',state:'Maharashtra',city:'Pune',turnover:'Above ₹5 Cr',udyam:'UDYAM-MH-00-5678901',employees:'50-100',desc:'Structural steel fabrication and supply for construction, infrastructure, and industrial projects.',contact:'+91 9800000005',email:'ops@mehtasteel.com',website:'mehtasteel.com',established:'2005',verified:true},
  {id:6,name:'Nisha Fashion Hub',initials:'NF',type:'Retailer',sector:'Textile & Apparel',state:'Delhi',city:'New Delhi',turnover:'₹10L – ₹50L',udyam:'UDYAM-DL-00-6789012',employees:'5-10',desc:'Women ethnic wear boutique sourcing handloom and block-print fabrics directly from artisan clusters.',contact:'+91 9800000006',email:'nisha@fashionhub.in',website:'fashionhub.in',established:'2019',verified:false},
  {id:7,name:'Reddy Pharma Dist.',initials:'RP',type:'Trading / Distribution',sector:'Healthcare',state:'Telangana',city:'Hyderabad',turnover:'₹50L – ₹1 Cr',udyam:'UDYAM-TS-00-7890123',employees:'10-25',desc:'Licensed pharmaceutical distributor servicing 200+ retail pharmacies and hospitals across Telangana.',contact:'+91 9800000007',email:'reddypharma@gmail.com',website:'reddypharma.com',established:'2014',verified:true},
  {id:8,name:'Green Build Infra',initials:'GB',type:'Service Provider',sector:'Construction',state:'Tamil Nadu',city:'Chennai',turnover:'₹1 Cr – ₹5 Cr',udyam:'UDYAM-TN-00-8901234',employees:'25-50',desc:'Green building consultant and contractor specialising in sustainable residential and commercial projects.',contact:'+91 9800000008',email:'info@greenbuild.in',website:'greenbuild.in',established:'2011',verified:true},
  {id:9,name:'Singh Transport',initials:'SiT',type:'Service Provider',sector:'Services',state:'Punjab',city:'Ludhiana',turnover:'₹50L – ₹1 Cr',udyam:'UDYAM-PB-00-9012345',employees:'10-25',desc:'Last-mile logistics and freight solutions across North India. Fleet of 40+ vehicles, real-time tracking.',contact:'+91 9800000009',email:'fleet@singhtransport.com',website:'singhtransport.com',established:'2009',verified:true},
  {id:10,name:'EduBridge Academy',initials:'EB',type:'Service Provider',sector:'Education & Training',state:'Uttar Pradesh',city:'Lucknow',turnover:'Below ₹10 Lakh',udyam:'UDYAM-UP-00-0123456',employees:'5-10',desc:'Vocational training and skill development center certified under NSDC for youth employment.',contact:'+91 9800000010',email:'admin@edubridge.in',website:'edubridge.in',established:'2020',verified:false},
  {id:11,name:'Kapoor Retail Mart',initials:'KR',type:'Retailer',sector:'Retail',state:'Haryana',city:'Faridabad',turnover:'₹10L – ₹50L',udyam:'UDYAM-HR-00-1122334',employees:'5-10',desc:'Multi-category general trade store serving 5,000+ daily customers. Expanding into online channel.',contact:'+91 9800000011',email:'kapoor@retailmart.in',website:'retailmart.in',established:'2016',verified:false},
  {id:12,name:'Bhatt Chemicals',initials:'BC',type:'Manufacturer',sector:'Manufacturing',state:'Rajasthan',city:'Jodhpur',turnover:'₹1 Cr – ₹5 Cr',udyam:'UDYAM-RJ-00-2233445',employees:'25-50',desc:'Industrial chemical manufacturer for paints, coatings, and textile processing industries.',contact:'+91 9800000012',email:'sales@bhattchem.com',website:'bhattchem.com',established:'2008',verified:true},
  {id:13,name:'Arora Digital Media',initials:'AD',type:'Service Provider',sector:'Technology',state:'Delhi',city:'Gurugram',turnover:'₹10L – ₹50L',udyam:'UDYAM-DL-00-3344556',employees:'5-10',desc:'Digital marketing agency for MSMEs — SEO, social media, WhatsApp campaigns, and brand identity.',contact:'+91 9800000013',email:'hello@aroradigital.in',website:'aroradigital.in',established:'2018',verified:false},
  {id:14,name:'Verma Cold Storage',initials:'VC',type:'Service Provider',sector:'Food & Agriculture',state:'Madhya Pradesh',city:'Indore',turnover:'₹50L – ₹1 Cr',udyam:'UDYAM-MP-00-4455667',employees:'10-25',desc:'Modern cold chain storage and logistics for perishables, fruits, and vegetables across Central India.',contact:'+91 9800000014',email:'ops@vermacold.in',website:'vermacold.in',established:'2013',verified:true},
  {id:15,name:'Joshi Auto Parts',initials:'JA',type:'Trading / Distribution',sector:'Manufacturing',state:'Maharashtra',city:'Nashik',turnover:'₹50L – ₹1 Cr',udyam:'UDYAM-MH-00-5566778',employees:'10-25',desc:'Authorised distributor for OEM auto parts serving garages and service stations across Maharashtra.',contact:'+91 9800000015',email:'joshi@autoparts.in',website:'joshiauto.in',established:'2007',verified:true},
  {id:16,name:'Rani Handicrafts',initials:'RH',type:'Manufacturer',sector:'Textile & Apparel',state:'West Bengal',city:'Kolkata',turnover:'Below ₹10 Lakh',udyam:'UDYAM-WB-00-6677889',employees:'1-5',desc:'Handloom sarees and artisan craft products connecting rural weavers to urban and export markets.',contact:'+91 9800000016',email:'rani@handicrafts.in',website:'ranihandicrafts.in',established:'2021',verified:false},
  {id:17,name:'Srinivas IT Park',initials:'SI',type:'Service Provider',sector:'Technology',state:'Telangana',city:'Hyderabad',turnover:'Above ₹5 Cr',udyam:'UDYAM-TS-00-7788990',employees:'50-100',desc:'IT infrastructure and managed services provider for mid-market and enterprise clients.',contact:'+91 9800000017',email:'info@srinivasit.com',website:'srinivasit.com',established:'2006',verified:true},
  {id:18,name:'Dixit Medical Equip.',initials:'DM',type:'Trading / Distribution',sector:'Healthcare',state:'Karnataka',city:'Bengaluru',turnover:'₹1 Cr – ₹5 Cr',udyam:'UDYAM-KA-00-8899001',employees:'10-25',desc:'Medical device and diagnostic equipment distributor for hospitals, clinics, and diagnostic labs.',contact:'+91 9800000018',email:'sales@dixitmedical.in',website:'dixitmedical.in',established:'2011',verified:true},
  {id:19,name:'Trivedi Constructions',initials:'TC',type:'Service Provider',sector:'Construction',state:'Gujarat',city:'Vadodara',turnover:'₹1 Cr – ₹5 Cr',udyam:'UDYAM-GJ-00-9900112',employees:'25-50',desc:'Civil contractor specialising in MSME factory sheds, warehouses, and commercial interiors.',contact:'+91 9800000019',email:'trivedi@constructions.in',website:'trivediconstructions.in',established:'2010',verified:true},
  {id:20,name:'Mukesh Dairy Farm',initials:'MD',type:'Manufacturer',sector:'Food & Agriculture',state:'Rajasthan',city:'Jaipur',turnover:'₹10L – ₹50L',udyam:'UDYAM-RJ-00-1011121',employees:'5-10',desc:'Hygienic dairy processing unit producing fresh milk, paneer, and ghee with direct farm-to-door delivery.',contact:'+91 9800000020',email:'mukesh@dairyfarm.in',website:'mukeshdairy.in',established:'2017',verified:false},
  {id:21,name:'Chopra Packaging',initials:'CP',type:'Manufacturer',sector:'Manufacturing',state:'Punjab',city:'Amritsar',turnover:'₹50L – ₹1 Cr',udyam:'UDYAM-PB-00-1213141',employees:'10-25',desc:'Custom corrugated box and flexible packaging solutions for FMCG, pharma, and e-commerce companies.',contact:'+91 9800000021',email:'sales@choprapack.in',website:'choprapack.in',established:'2014',verified:true},
  {id:22,name:'Iyer Consulting',initials:'IC',type:'Service Provider',sector:'Services',state:'Tamil Nadu',city:'Chennai',turnover:'₹10L – ₹50L',udyam:'UDYAM-TN-00-1314151',employees:'1-5',desc:'GST compliance, MSME registration, and government scheme advisory for small businesses.',contact:'+91 9800000022',email:'iyerconsult@gmail.com',website:'iyerconsulting.in',established:'2019',verified:false},
  {id:23,name:'Bansal Furniture',initials:'BF',type:'Manufacturer',sector:'Manufacturing',state:'Uttar Pradesh',city:'Agra',turnover:'₹50L – ₹1 Cr',udyam:'UDYAM-UP-00-1415161',employees:'10-25',desc:'Office and home furniture manufacturer offering modular and custom-built solutions across North India.',contact:'+91 9800000023',email:'bansal@furniture.in',website:'bansalfurniture.in',established:'2012',verified:true},
  {id:24,name:'Anand Solar Energy',initials:'AS',type:'Service Provider',sector:'Technology',state:'Maharashtra',city:'Nagpur',turnover:'₹10L – ₹50L',udyam:'UDYAM-MH-00-1516171',employees:'5-10',desc:'Solar panel installation and maintenance for residential, commercial, and industrial MSME clients.',contact:'+91 9800000024',email:'info@anandsolar.in',website:'anandsolar.in',established:'2020',verified:true},
];

@Component({
  selector: 'app-connect',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './connect.html',
  styleUrl: './connect.css',
})
export class Connect {
  private readonly session = inject(AuthSessionService);
  private readonly payments = inject(PaymentService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly search = signal('');
  readonly sector = signal('');
  readonly state = signal('');
  readonly turnover = signal('');

  readonly planLoading = signal(false);
  readonly activePlan = signal<ActivePlan | null>(null);
  readonly isUnlocked = computed(() => !!this.activePlan());

  readonly upgradeOpen = signal(false);
  readonly selectedUpgradePlan = signal<'premium' | 'pro'>('premium');

  readonly profileOpen = signal(false);
  readonly currentProfile = signal<Msme | null>(null);

  readonly filtered = computed(() => {
    const q = this.search().trim().toLowerCase();
    const sector = this.sector();
    const state = this.state();
    const turnover = this.turnover();
    return MSME_DATA.filter((m) => {
      const matchQ =
        !q ||
        m.name.toLowerCase().includes(q) ||
        m.sector.toLowerCase().includes(q) ||
        m.city.toLowerCase().includes(q) ||
        m.desc.toLowerCase().includes(q);
      const matchSector = !sector || m.sector === sector;
      const matchState = !state || m.state === state;
      const matchTurn = !turnover || m.turnover === turnover;
      return matchQ && matchSector && matchState && matchTurn;
    });
  });

  readonly countLabel = computed(() => this.filtered().length);

  constructor() {
    effect(() => {
      const userId = this.session.userId();
      if (!userId) {
        this.activePlan.set(null);
        return;
      }
      void this.loadPlan(userId);
    });
  }

  private async loadPlan(userId: string) {
    try {
      this.planLoading.set(true);
      const res = await firstValueFrom(this.payments.myPlan().pipe(timeout(15000)));
      this.activePlan.set(res?.plan ?? null);
    } catch {
      this.activePlan.set(null);
    } finally {
      this.planLoading.set(false);
    }
  }

  openUpgrade() {
    this.upgradeOpen.set(true);
  }

  closeUpgrade() {
    this.upgradeOpen.set(false);
  }

  proceedToUpgrade() {
    // Store intended plan code and redirect to membership (will login-gate + open checkout)
    try {
      window.localStorage.setItem('mbm_pending_plan', this.selectedUpgradePlan());
    } catch {}
    this.closeUpgrade();
    this.router.navigateByUrl('/membership');
  }

  openProfile(m: Msme) {
    this.currentProfile.set(m);
    this.profileOpen.set(true);
  }

  closeProfile() {
    this.profileOpen.set(false);
    this.currentProfile.set(null);
  }

  connectTo(m: Msme) {
    if (!this.session.isLoggedIn()) {
      try {
        window.localStorage.setItem('mbm_pending_after_login', '/connect');
      } catch {}
      this.toast.info('Please login to use MSME Connect.');
      this.router.navigateByUrl('/login');
      return;
    }
    if (!this.isUnlocked()) {
      this.openUpgrade();
      return;
    }
    this.toast.success(`Connection request sent to ${m.name}.`);
  }

  avatarColor(id: number): string {
    const colors = ['#C8102E', '#0B1C3D', '#16a34a', '#7c3aed', '#d97706', '#0891b2'];
    return colors[id % colors.length];
  }
}

