// firebase.js
import { initializeApp } from 'firebase/app';
import { getStorage } from 'firebase/storage';

// const firebaseConfig = {
//   apiKey: process.env.NEXT_PUBLIC_FIREBASE_API_KEY,
//   authDomain: process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN,
//   projectId: process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID,
//   storageBucket: process.env.NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET,
//   messagingSenderId: process.env.NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID,
//   appId: process.env.NEXT_PUBLIC_FIREBASE_APP_ID,
// };

const firebaseConfig = {
    apiKey: "AIzaSyAqwFNkAKe6TTHrTqp8s-SFE_eM7qa3XTU",
    authDomain: "easytrip-36fb6.firebaseapp.com",
    projectId: "easytrip-36fb6",
    storageBucket: "easytrip-36fb6.appspot.com",
    messagingSenderId: "216132493795",
    appId: "1:216132493795:web:d92cf674209712035600d6",
    measurementId: "G-0WCWL93QZM"
  };

const app = initializeApp(firebaseConfig);
const storage = getStorage(app);

export { storage };
