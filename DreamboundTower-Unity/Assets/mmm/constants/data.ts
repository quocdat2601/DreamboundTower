import { Icons } from "@/components/icons";
import { NavItem, SidebarNavItem } from "@/types";

export type User = {
  id: number;
  name: string;
  company: string;
  role: string;
  verified: boolean;
  status: string;
};

export type Stock = { 
  symbol: string;
  name: string;
  price: number;
  change: number;
  changesPercentage: number;
};


export type Order = {
  id : string;
  symbol : string;
  side : string;
  type : string; 
  qty : number;
  avg_cost : number;
  amount : number; 
  status : string;
  date : string;
  limit_price : number;
  stop_price : number;
  filled_qty? : number;
  filled_avg_price? : number;
}; 

export const navItems: NavItem[] = [
  
  {
    title: "Trip",
    href: "/trip",
    icon: "trip",
    label: "Trip",
  }, 
 
  {
    title: "Spots in Map",
    href: "/trip/maps",
    icon: "places",
    label: "Maps",
  },

  {
    title: "Photos and Videos",
    href: "/trip/media",
    icon: "photos",
    label: "media",
  },
  {
    title: "Ai Tour Helper",
    href: "/trip/ai-bot",
    icon: "analytics",
    label: "Ai Helper",
  },

  {
    title: "Blogs",
    href: "/trip/blogs",
    icon: "blogs",
    label: "Blogs",
  },

  {
    title: "Vlogs",
    href: "/trip/vlogs",
    icon: "vlogs",
    label: "Vlogs",
  },
 
  {
    title: "Setting",
    href: "/trip/settings",
    icon: "Settings",
    label: "setting",
  },
 
 
];

 