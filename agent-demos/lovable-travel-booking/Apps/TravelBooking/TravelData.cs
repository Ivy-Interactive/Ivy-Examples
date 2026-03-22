namespace LovableTravelBooking.Apps.TravelBooking;

public static class TravelData
{
    public static readonly TravelPackage[] Packages =
    [
        new(1, "Bali Paradise", "Bali, Indonesia",
            "https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=800&q=80",
            899m, "7 days", 4.8, "Experience the magic of Bali with pristine beaches, ancient temples, and lush rice terraces.", "Beach"),

        new(2, "Maldives Retreat", "Maldives",
            "https://images.unsplash.com/photo-1514282401047-d79a71a590e8?w=800&q=80",
            1499m, "5 days", 4.9, "Luxury overwater villas, crystal-clear waters, and world-class snorkeling in paradise.", "Beach"),

        new(3, "Santorini Escape", "Santorini, Greece",
            "https://images.unsplash.com/photo-1570077188670-e3a8d69ac5ff?w=800&q=80",
            1199m, "6 days", 4.7, "Iconic white-washed buildings, stunning sunsets, and Mediterranean charm.", "Beach"),

        new(4, "Swiss Alps Adventure", "Switzerland",
            "https://images.unsplash.com/photo-1531366936337-7c912a4589a7?w=800&q=80",
            1299m, "8 days", 4.8, "Breathtaking mountain scenery, world-class skiing, and charming alpine villages.", "Mountain"),

        new(5, "Patagonia Trek", "Patagonia, Argentina",
            "https://images.unsplash.com/photo-1464278533981-50106e6176b1?w=800&q=80",
            1599m, "10 days", 4.6, "Epic trekking through Torres del Paine, glaciers, and untamed wilderness.", "Mountain"),

        new(6, "Nepal Himalayas", "Nepal",
            "https://images.unsplash.com/photo-1544735716-392fe2489ffa?w=800&q=80",
            999m, "12 days", 4.5, "Trek to Everest Base Camp through stunning Himalayan landscapes and Sherpa villages.", "Mountain"),

        new(7, "Tokyo Discovery", "Tokyo, Japan",
            "https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?w=800&q=80",
            1099m, "7 days", 4.7, "Explore the perfect blend of ancient traditions and cutting-edge technology.", "City"),

        new(8, "Paris Romance", "Paris, France",
            "https://images.unsplash.com/photo-1502602898657-3e91760cbb34?w=800&q=80",
            1399m, "5 days", 4.8, "The city of love awaits with iconic landmarks, world-class cuisine, and art.", "City"),

        new(9, "New York Explorer", "New York, USA",
            "https://images.unsplash.com/photo-1496442226666-8d4d0e62e6e9?w=800&q=80",
            899m, "4 days", 4.6, "Experience the energy of the Big Apple with Broadway, Central Park, and iconic skylines.", "City"),

        new(10, "Safari Kenya", "Kenya",
            "https://images.unsplash.com/photo-1547471080-7cc2caa01a7e?w=800&q=80",
            1799m, "9 days", 4.9, "Witness the Great Migration and encounter Africa's magnificent wildlife up close.", "Adventure"),

        new(11, "Costa Rica Rainforest", "Costa Rica",
            "https://images.unsplash.com/photo-1518259102261-b40117eabbc0?w=800&q=80",
            1099m, "7 days", 4.7, "Zip-line through canopies, spot exotic wildlife, and relax in natural hot springs.", "Adventure"),

        new(12, "Iceland Northern Lights", "Iceland",
            "https://images.unsplash.com/photo-1504829857797-ddff29c27927?w=800&q=80",
            1599m, "6 days", 4.8, "Chase the aurora borealis, explore glaciers, and bathe in geothermal lagoons.", "Adventure"),
    ];
}
