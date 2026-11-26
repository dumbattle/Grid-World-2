namespace GridWorld {
    public readonly struct CutsceneRequest {
        public readonly string id;
        public readonly CutsceneStart start;

        public CutsceneRequest(string id, CutsceneStart start) {
            this.id = id;
            this.start = start;
        }
    }
}
